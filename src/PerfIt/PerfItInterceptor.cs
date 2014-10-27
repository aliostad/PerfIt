using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PerfIt
{
    public class PerfItInterceptor : IInterceptor
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

            var filter = PerfItRuntime.FindPerfItAttribute(invocation.MethodInvocationTarget);
            if (!PerfItRuntime.PublishCounters || filter == null)
            {

                invocation.Proceed();

            }
            else
            {
                try
                {

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



                    try
                    {
                        invocation.Proceed();


                        var returnType = invocation.Method.ReturnType;
                        if (returnType != typeof(void))
                        {
                            var returnValue = invocation.ReturnValue;
                            if (returnType == typeof(Task))
                            {
                                var task = (Task)returnValue;
                                task.ContinueWith((antecedent) =>
                                {
                                    WrapUpCounters(filter, invocationContext);
                                });
                            }
                            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                            {

                                var task = (Task)returnValue;
                                task.ContinueWith((antecedent) =>
                                {
                                    WrapUpCounters(filter, invocationContext);
                                });
                            }
                            else
                            {
                                // Log.Debug("Returning with: " + returnValue);
                                WrapUpCounters(filter, invocationContext);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        //if (Log.IsErrorEnabled) Log.Error(CreateInvocationLogString("ERROR", invocation), ex);
                        throw;
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

        private static void WrapUpCounters(IPerfItAttribute filter, PerfItContext invocationContext)
        {
            foreach (var counter in filter.Counters)
            {
                PerfItRuntime.MonitoredCountersContexts[PerfItRuntime.GetUniqueName(filter.InstanceName, counter)].Handler.OnRequestEnding(invocationContext);
            }
        }


    }
}
