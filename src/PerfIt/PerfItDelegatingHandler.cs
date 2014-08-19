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
        private Dictionary<string, PerfItCounterContext> _counterContexts = 
            new Dictionary<string, PerfItCounterContext>();

        private readonly string _applicationName;
        private bool _publish = false;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryName">Name of the grouping category of counters (e.g. Process, Processor, Network Interface are all categories)
        /// if not provided, it will use name of the assembly.
        /// </param>
        public PerfItDelegatingHandler(string categoryName = null)
        {
   

            SetPublish();
            SetErrorPolicy();

            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = assembly.GetName().Name;

            var filters = PerfItRuntime.FindAllFilters(assembly);
            foreach (var filter in filters)
            {
                foreach (var counterType in filter.Counters)
                {
                    if(!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not registered: " + counterType);

                    var counterHandler = PerfItRuntime.HandlerFactories[counterType](categoryName, filter.InstanceName, filter);
                    if (!_counterContexts.Keys.Contains(counterHandler.UniqueName))
                    {
                        _counterContexts.Add(counterHandler.UniqueName, new PerfItCounterContext()
                                                                             {
                                                                                 Handler = counterHandler,
                                                                                 Name = counterHandler.UniqueName
                                                                             });
                    }
                }
                    
            }

        }

        private void SetPublish()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishCounters] ?? "true";
            _publish = Convert.ToBoolean(value);
        }

        protected void SetErrorPolicy()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishErrors] ?? "true";
            PerfItRuntime.ThrowPublishingErrors = Convert.ToBoolean(value);
        }

        public string ApplicationName
        {
            get { return _applicationName; }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            try
            {
                // check whether turned off in config

                if (!_publish)
                    return base.SendAsync(request, cancellationToken);
                
                request.Properties.Add(Constants.PerfItKey, new PerfItContext());
                foreach (var context in _counterContexts.Values)
                {
                    context.Handler.OnRequestStarting(request);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());

                if(PerfItRuntime.ThrowPublishingErrors)
                    throw exception;
            }
            

            return base.SendAsync(request, cancellationToken)
                .Then((response) => 
                        {
                            try
                            {
                                var ctx = (PerfItContext)response.RequestMessage.Properties[Constants.PerfItKey];

                                foreach (var counter in ctx.CountersToRun)
                                {
                                    _counterContexts[counter].Handler.OnRequestEnding(response);
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.TraceError(e.ToString());
                                if(PerfItRuntime.ThrowPublishingErrors)
                                    throw e;
                            }
                            
                            return response;

                        }, cancellationToken);

        }

        private class PerfItCounterContext
        {
            public string Name { get; set; }
            public ICounterHandler Handler { get; set; }

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var context in _counterContexts.Values)
                {
                    context.Handler.Dispose();
                }
                _counterContexts.Clear();
            }
        }
    }
}
