using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using System.Reflection;
using System.Collections.Concurrent;

namespace PerfIt
{
    public class PerfItInterceptor: IInterceptor
    {

        
        

        private string CategoryName { get; set; }

        


        public PerfItInterceptor(string categoryName)
        {
           

            //Build all the counter handlers beforehand
            var frames = new StackTrace().GetFrames();
            var monitoredType = frames[1].GetType();
            if (string.IsNullOrEmpty(categoryName))
                categoryName = monitoredType.Assembly.GetName().Name;

            CategoryName = categoryName;
            
        }
        


        public void Intercept(IInvocation invocation)
        {
            

            if (!PerfItRuntime.PublishCounters)
            {

                invocation.Proceed();

            }
            else
            {
                try
                {
                    var filter = PerfItRuntime.FindPerfItAttribute(invocation.MethodInvocationTarget);
                    var invocationContext = new PerfItContext();
                    if (filter != null)
                    {
                        foreach (var counterType in filter.Counters)
                        {
                            if (!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                                throw new ArgumentException("Counter type not registered: " + counterType);

                            var counterHandler = PerfItRuntime.HandlerFactories[counterType](CategoryName.ToString(), filter.InstanceName);
                            if (!PerfItRuntime.MonitoredCountersContexts.Keys.Contains(counterHandler.UniqueName))
                            {
                                PerfItRuntime.MonitoredCountersContexts.AddOrUpdate(counterHandler.UniqueName, new PerfItCounterContext()
                                {
                                    Handler = counterHandler,
                                    Name = counterHandler.UniqueName
                                }, (key, existingCounter) => existingCounter);
                            }
                        }
                    }
                    foreach (var counter in filter.Counters)
                    {
                        PerfItRuntime.MonitoredCountersContexts[PerfItRuntime.GetUniqueName(filter.InstanceName, counter)].Handler.OnRequestStarting(invocationContext);
                    }


                    invocation.Proceed();

                    foreach (var counter in filter.Counters)
                    {
                        PerfItRuntime.MonitoredCountersContexts[PerfItRuntime.GetUniqueName(filter.InstanceName, counter)].Handler.OnRequestEnding(invocationContext);
                    }
                }
                catch (Exception exception)
                {
                    Trace.TraceError(exception.ToString());

                    if (PerfItRuntime.RaisePublishErrors)
                        throw exception;
                }


               


            }
        }

       
    }
}
