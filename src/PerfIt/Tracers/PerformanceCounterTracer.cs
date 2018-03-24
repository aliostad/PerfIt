#if NET452
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PerfIt
{
    public class PerformanceCounterTracer : ITwoStageTracer
    {
        private readonly IInstrumentationInfo _info;

        private ConcurrentDictionary<string, Lazy<PerfitHandlerContext>> _counterContexts =
            new ConcurrentDictionary<string, Lazy<PerfitHandlerContext>>();

        public PerformanceCounterTracer(IInstrumentationInfo instrumentationInfo)
        {
            _info = instrumentationInfo;
        }

        public void Finish(object token, long timeTakenMilli, string correlationId = null, InstrumentationContext extraContext = null)
        {
            var contexts = (Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>>) token;
            CompleteContexts(contexts);
        }

        public object Start(IInstrumentationInfo info)
        {
            return BuildContexts();
        }

        private Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> BuildContexts()
        {
            var contexts = new List<PerfitHandlerContext>();
            Prepare(contexts);

            var ctx = new Dictionary<string, object>();

            ctx.Add(Constants.PerfItKey, new PerfItContext());
            ctx.Add(Constants.PerfItPublishErrorsKey, _info.RaisePublishErrors);
            foreach (var context in contexts)
            {
                try
                {
                    context.Handler.OnRequestStarting(ctx);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    if (_info.RaisePublishErrors)
                        throw;
                }
            }

            return new Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>>(contexts, ctx);
        }

        private void CompleteContexts(Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> contexts)
        {
            try
            {
                foreach (var counter in contexts.Item1)
                {
                    counter.Handler.OnRequestEnding(contexts.Item2);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (_info.RaisePublishErrors)
                    throw;
            }
        }

        private void Prepare(List<PerfitHandlerContext> contexts)
        {
            var counters = _info.Counters == null || _info.Counters.Length == 0 ? CounterTypes.StandardCounters : _info.Counters;

            foreach (var handlerFactory in PerfItRuntime.HandlerFactories.Where(c => counters.Contains(c.Key)))
            {
                var key = GetKey(handlerFactory.Key, _info.InstanceName);
                var ctx = _counterContexts.GetOrAdd(key, k =>
                    new Lazy<PerfitHandlerContext>(() => new PerfitHandlerContext()
                    {
                        Handler = handlerFactory.Value(_info.CategoryName, _info.InstanceName),
                        Name = _info.InstanceName
                    }));
                contexts.Add(ctx.Value);
            }
        }

        private string GetKey(string counterName, string instanceName)
        {
            return string.Format("{0}_{1}", counterName, instanceName);
        }

        public void Dispose()
        {
            foreach (var context in _counterContexts.Values)
            {
                context.Value.Handler.Dispose();
            }

            _counterContexts.Clear();
        }
    }
}
#endif