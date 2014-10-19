using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace PerfIt
{
    public class PerfItDelegatingHandler : DelegatingHandler
    {
       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryName">Name of the grouping category of counters (e.g. Process, Processor, Network Interface are all categories)
        /// if not provided, it will use name of the assembly.
        /// </param>
        public PerfItDelegatingHandler(string categoryName = null)
        {

            

            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = assembly.GetName().Name;

            var filters = PerfItRuntime.FindAllPerfItAttributes(assembly);
            foreach (var filter in filters)
            {
                foreach (var counterType in filter.Counters)
                {
                    if(!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not registered: " + counterType);

                    var counterHandler = PerfItRuntime.HandlerFactories[counterType](categoryName, filter.InstanceName);
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

        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            
            if (!PerfItRuntime.PublishCounters)
                return base.SendAsync(request, cancellationToken);
            
                      

            return base.SendAsync(request, cancellationToken)
                .Then((response) => 
                        {
                            try
                            {

                                if (response.RequestMessage.Properties.Keys.Contains(Constants.PerfItKey))
                                {
                                    var invocationContext = (PerfItContext)response.RequestMessage.Properties[Constants.PerfItKey];

                                    foreach (var counter in invocationContext.CountersToRun)
                                    {
                                        PerfItRuntime.MonitoredCountersContexts[counter].Handler.OnRequestEnding(invocationContext);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.TraceError(e.ToString());
                                if(PerfItRuntime.RaisePublishErrors)
                                    throw e;
                            }
                            
                            return response;

                        }, cancellationToken);

        }

     

        
    }

}
