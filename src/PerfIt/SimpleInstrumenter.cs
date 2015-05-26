using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PerfIt
{
    public class SimpleInstrumenter : IInstrumenter, IDisposable
    {
        private IInstrumentationInfo _info;
        private string _categoryName;

        private ConcurrentDictionary<string, Lazy<PerfitHandlerContext>> _counterContexts =
          new ConcurrentDictionary<string, Lazy<PerfitHandlerContext>>();

        public SimpleInstrumenter(IInstrumentationInfo info, string categoryName, 
            bool publishCounters = true, 
            bool publishEvent = true,
            bool raisePublishErrors = false)
        {
            _categoryName = categoryName;
            _info = info;

            PublishCounters = publishCounters;
            RaisePublishErrors = raisePublishErrors;
            PublishEvent = publishEvent;
        }

        public void Instrument(Action aspect, string instrumentationContext = null)
        {
            if (!PublishCounters)
                aspect();

            var contexts = BuildContexts();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                aspect();
            }
            finally
            {
                if (PublishEvent)
                {
                    InstrumentationEventSource.Instance.WriteInstrumentationEvent(_categoryName,
                        _info.InstanceName, stopwatch.ElapsedMilliseconds, instrumentationContext);
                }
            }
           
            CompleteContexts(contexts);
          
        }

        public async Task InstrumentAsync(Func<Task> asyncAspect, string instrumentationContext = null)
        {
            if (!PublishCounters)
                await asyncAspect();

            var contexts = BuildContexts();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await asyncAspect();
            }
            finally
            {
                if (PublishEvent)
                {
                    InstrumentationEventSource.Instance.WriteInstrumentationEvent(_categoryName,
                        _info.InstanceName, stopwatch.ElapsedMilliseconds, instrumentationContext);
                }
            }
            
            CompleteContexts(contexts);
           
        }

        private Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> BuildContexts()
        {
            var contexts = new List<PerfitHandlerContext>();
            Prepare(contexts);

            var ctx = new Dictionary<string, object>();

            ctx.Add(Constants.PerfItKey, new PerfItContext());
            ctx.Add(Constants.PerfItPublishErrorsKey, this.RaisePublishErrors);
            foreach (var context in contexts)
            {
                context.Handler.OnRequestStarting(ctx);
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
                if (RaisePublishErrors)
                    throw;
            }
        }

        private void Prepare(List<PerfitHandlerContext> contexts)
        {
            foreach (var handlerFactory in PerfItRuntime.HandlerFactories)
            {
                var key = GetKey(handlerFactory.Key, _info.InstanceName);
                var ctx = _counterContexts.GetOrAdd(key, k =>
                    new Lazy<PerfitHandlerContext>(() => new PerfitHandlerContext()
                    {
                        Handler = handlerFactory.Value(_categoryName, _info.InstanceName),
                        Name = _info.InstanceName
                    }));
                contexts.Add(ctx.Value);
            }
        }

        private string GetKey(string counterName, string instanceName)
        {
            return string.Format("{0}_{1}", counterName, instanceName);
        }

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

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
