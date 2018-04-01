using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PerfIt.Http
{

    /// <summary>
    /// Delegating handler for HttpClient
    /// </summary>
    public class PerfitClientDelegatingHandler : DelegatingHandler, IInstrumentationInfo
    {

        public string CorrelationIdKey { set; get; }

        private ConcurrentDictionary<string, SimpleInstrumentor>
            _instrumenters = new ConcurrentDictionary<string, SimpleInstrumentor>();

        public PerfitClientDelegatingHandler(string categoryName, params ITwoStageTracer[] tracers)
        {
            Tracers = tracers.ToList();
            CategoryName = categoryName;
            CorrelationIdKey = Correlation.CorrelationIdKey;
#if NET452
            PublishCounters = true;
#endif
            RaisePublishErrors = true;
            PublishEvent = true;
            SamplingRate = Constants.DefaultSamplingRate;
            InstanceName = null;

            SetErrorPolicy();
#if NET452
            SetPublish();
#endif
            SetEventPolicy();
            SetSamplingRate();

#if NET452
            Counters = PerfItRuntime.HandlerFactories.Keys.ToArray();
#endif
            InstanceNameProvider = request =>
                string.Format("{0}_{1}", request.Method.Method.ToLower(), request.RequestUri.Host.ToLower());            
        }

        public string InstanceName { get; set; }
        public string Description { get; set; }
        public string[] Counters { get; set; }
        public string CategoryName { get; set; }

#if NET452
        public bool PublishCounters { get; set; }
#endif
        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

        public double SamplingRate { get; set; }
        string IInstrumentationInfo.CorrelationIdKey { get; set; }

        public List<ITwoStageTracer> Tracers { get; }

        /// <summary>
        /// Provides the performance counter instance name.
        /// Default impl combines method and the host name of the request.
        /// </summary>
        public Func<HttpRequestMessage, string> InstanceNameProvider { get; set; }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var instanceName = InstanceName ?? InstanceNameProvider(request);
            var instrumenter =_instrumenters.GetOrAdd(instanceName, 
                (insName) =>
                {
                    var inst = new SimpleInstrumentor(new InstrumentationInfo()
                    {
                        Description = "Counter for " + insName,
#if NET452
                        Counters = Counters,
                        PublishCounters = PublishCounters,
#endif
                        InstanceName = insName,
                        CategoryName = CategoryName,
                        SamplingRate = SamplingRate,
                        RaisePublishErrors = RaisePublishErrors,
                        CorrelationIdKey = CorrelationIdKey
                    });

                    foreach (var tracer in Tracers)
                    {
                        inst.Tracers.Add(tracer.GetType().FullName, tracer);
                    }

                    return inst;
                }
            );

            HttpResponseMessage response = null;

            Func<Task> t = async () => response = await base.SendAsync(request, cancellationToken);

            var ctx = new InstrumentationContext() { Text1 = request.RequestUri.AbsoluteUri };
            await instrumenter.InstrumentAsync(t, SamplingRate, ctx);
            return response;
        }

#if NET452
        private void SetPublish()
        {
            PublishCounters = PerfItRuntime.IsPublishCounterEnabled(CategoryName, PublishCounters);
        }
#endif
        protected void SetErrorPolicy()
        {
            RaisePublishErrors = PerfItRuntime.IsPublishErrorsEnabled(CategoryName, RaisePublishErrors);
        }

        protected void SetEventPolicy()
        {
            PublishEvent = PerfItRuntime.IsPublishEventsEnabled(CategoryName, PublishEvent);
        }

        protected void SetSamplingRate()
        {
            SamplingRate = PerfItRuntime.GetSamplingRate(CategoryName, SamplingRate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var instrumenter in _instrumenters.Values)
                {
                    instrumenter.Dispose();
                }

                _instrumenters.Clear();
            }
        }
    }
}
