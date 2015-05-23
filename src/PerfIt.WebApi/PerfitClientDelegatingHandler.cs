using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
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
            PublishCounters = true;
            RaisePublishErrors = true;

            SetErrorPolicy();
            SetPublish();

            InstanceNameProvider = request =>
                string.Format("{0}_{1}", request.Method.Method.ToLower(), request.RequestUri.Host.ToLower());
        }

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        private string GetKey(string counterName, string instanceName)
        {
            return string.Format("{0}_{1}", counterName, instanceName);
        }

        /// <summary>
        /// Provides the performance counter instance name.
        /// Default impl combines method and the host name of the request.
        /// </summary>
        public Func<HttpRequestMessage, string> InstanceNameProvider { get; set; }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (!PublishCounters)
                return await base.SendAsync(request, cancellationToken);

            var instanceName = InstanceNameProvider(request);

            var contexts = new List<PerfItCounterContext>();
            foreach (var handlerFactory in PerfItRuntime.HandlerFactories)
            {
                var key = GetKey(handlerFactory.Key, instanceName);
                var ctx = _counterContexts.GetOrAdd(key, k =>
                    new Lazy<PerfItCounterContext>(() => new PerfItCounterContext()
                    {
                        Handler = handlerFactory.Value(_categoryName, instanceName)
                    }));
                contexts.Add(ctx.Value);
            }

            request.Properties.Add(Constants.PerfItKey, new PerfItContext());
            request.Properties.Add(Constants.PerfItPublishErrorsKey, this.RaisePublishErrors);

            foreach (var context in contexts)
            {
                context.Handler.OnRequestStarting(request.Properties);
            }

            var response = await base.SendAsync(request, cancellationToken);
            try
            {
                foreach (var counter in contexts)
                {
                    counter.Handler.OnRequestEnding(response.RequestMessage.Properties);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (RaisePublishErrors)
                    throw;
            }

            return response;
        }


        private void SetPublish()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishCounters] ?? "true";
            PublishCounters = Convert.ToBoolean(value);
        }

        protected void SetErrorPolicy()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishErrors] ?? RaisePublishErrors.ToString();
            RaisePublishErrors = Convert.ToBoolean(value);
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
