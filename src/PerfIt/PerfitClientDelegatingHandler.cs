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

namespace PerfIt
{

    /// <summary>
    /// Delegating handler for HttpClient
    /// </summary>
    public class PerfitClientDelegatingHandler : DelegatingHandler
    {

        private ConcurrentDictionary<string, Lazy<PerfItCounterContext>> _counterContexts =
          new ConcurrentDictionary<string, Lazy<PerfItCounterContext>>();

        private string _categoryName;

        public PerfitClientDelegatingHandler(string categoryName)
        {
            _categoryName = categoryName;
        

            InstanceNameProvider = request =>
                string.Format("{0}_{1}", request.Method.Method.ToLower(), request.RequestUri.Host.ToLower());
        }


        private string GetKey(string counterName, string instanceName)
        {
            return string.Format("{0}_{1}", counterName, instanceName);
        }

        /// <summary>
        /// Provides the performance counter instance name.
        /// Default impl combines method and the host name of the request.
        /// </summary>
        public Func<HttpRequestMessage, string> InstanceNameProvider { get; set; }
           
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (!PerfItRuntime.PublishCounters)
                return base.SendAsync(request, cancellationToken);

            var instanceName = InstanceNameProvider(request);

            var contexts = new List<PerfItCounterContext>();
            foreach (var handlerFactory in PerfItRuntime.HandlerFactories)
            {
                var key = GetKey(handlerFactory.Key, instanceName);
                var ctx = _counterContexts.GetOrAdd(key, k => 
                    new Lazy<PerfItCounterContext>( () => new PerfItCounterContext()
                    {
                        Handler = handlerFactory.Value(_categoryName, instanceName)
                    } ));   
                contexts.Add(ctx.Value);
            }

            request.Properties.Add(Constants.PerfItKey, new PerfItContext());
            
            
            foreach (var context in contexts)
            {
                context.Handler.OnRequestStarting((PerfItContext)request.Properties[Constants.PerfItKey]);
            }

            return base.SendAsync(request, cancellationToken)
                .Then((response) => 
                        {
                            try
                            {

                                foreach (var counter in contexts)
                                {
                                    counter.Handler.OnRequestEnding((PerfItContext)response.RequestMessage.Properties[Constants.PerfItKey]);
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


      

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var context in _counterContexts.Values)
                {
                    context.Value.Handler.Dispose();
                }
                _counterContexts.Clear();
            }
        }
    }
}
