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

        

        private readonly string _applicationName;
        private bool _publish = false;
        private string _categoryName;

        public PerfitClientDelegatingHandler(string categoryName)
        {
            _categoryName = categoryName;

            SetErrorPolicy();
            SetPublish();
        }

        private string GetKey(string counterName, string instanceName)
        {
            return string.Format("{0}_{1}", counterName, instanceName);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (!_publish)
                return base.SendAsync(request, cancellationToken);

            var instanceName = request.RequestUri.Host;

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
                context.Handler.OnRequestStarting(request);
            }


            return base.SendAsync(request, cancellationToken)
                .Then((response) => 
                        {
                            try
                            {

                                foreach (var counter in contexts)
                                {
                                    counter.Handler.OnRequestEnding(response);
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
