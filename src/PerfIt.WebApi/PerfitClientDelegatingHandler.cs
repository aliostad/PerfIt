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

namespace PerfIt
{

    /// <summary>
    /// Delegating handler for HttpClient
    /// </summary>
    public class PerfitClientDelegatingHandler : DelegatingHandler, IInstrumentationInfo
    {

        public string CorrelationIdKey { set; get; }

        private ConcurrentDictionary<string, SimpleInstrumentor>
            _instrumenters = new ConcurrentDictionary<string, SimpleInstrumentor>();

        private ITwoStageTracer[] _tracers = new ITwoStageTracer[0];

        public PerfitClientDelegatingHandler(string categoryName, IEnumerable<ITwoStageTracer> tracers = null)
        {
            CategoryName = categoryName;
            CorrelationIdKey = Correlation.CorrelationIdKey;
            PublishCounters = true;
            RaisePublishErrors = true;
            PublishEvent = true;
            SamplingRate = Constants.DefaultSamplingRate;
            InstanceName = null;

            SetErrorPolicy();
            SetPublish();
            SetEventPolicy();
            SetSamplingRate();

            Counters = PerfItRuntime.HandlerFactories.Keys.ToArray();

            if (tracers != null)
                _tracers = tracers.ToArray();

            InstanceNameProvider = request =>
                string.Format("{0}_{1}", request.Method.Method.ToLower(), request.RequestUri.Host.ToLower());            
        }

        public string InstanceName { get; set; }
        public string Description { get; set; }
        public string[] Counters { get; set; }
        public string CategoryName { get; set; }
        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

        public double SamplingRate { get; set; }
        string IInstrumentationInfo.CorrelationIdKey { get; set; }

        /// <summary>
        /// Provides the performance counter instance name.
        /// Default impl combines method and the host name of the request.
        /// </summary>
        public Func<HttpRequestMessage, string> InstanceNameProvider { get; set; }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (!PublishCounters && !PublishEvent)
                return await base.SendAsync(request, cancellationToken);

            var instanceName = InstanceName ?? InstanceNameProvider(request);
            var instrumenter =_instrumenters.GetOrAdd(instanceName, 
                (insName) =>
                {
                    var inst = new SimpleInstrumentor(new InstrumentationInfo()
                    {
                        Counters = Counters,
                        Description = "Counter for " + insName,
                        InstanceName = insName,
                        CategoryName = CategoryName,
                        SamplingRate = SamplingRate,
                        PublishCounters = PublishCounters,
                        PublishEvent = PublishEvent,
                        RaisePublishErrors = RaisePublishErrors,
                        CorrelationIdKey = CorrelationIdKey
                    });

                    foreach (var tracer in _tracers)
                    {
                        inst.Tracers.Add(tracer.GetType().FullName, tracer);
                    }

                    return inst;
                }
            );

            HttpResponseMessage response = null;

            Func<Task> t = async () => response = await base.SendAsync(request, cancellationToken);

            await instrumenter.InstrumentAsync(t, request.RequestUri.AbsoluteUri, SamplingRate);
            return response;
        }


        private void SetPublish()
        {
            PublishCounters = PerfItRuntime.IsPublishCounterEnabled(CategoryName, PublishCounters);
        }

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
