using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PerfIt
{
    public class SimpleInstrumentor : IInstrumentor, ITwoStageInstrumentor, IDisposable
    {
        private IInstrumentationInfo _info;

        private ConcurrentDictionary<string, Lazy<PerfitHandlerContext>> _counterContexts =
          new ConcurrentDictionary<string, Lazy<PerfitHandlerContext>>();

        public SimpleInstrumentor(IInstrumentationInfo info, 
            bool publishCounters = true, 
            bool publishEvent = true,
            bool raisePublishErrors = false)
        {
            _info = info;

            PublishCounters = publishCounters;
            RaisePublishErrors = raisePublishErrors;
            PublishEvent = publishEvent;
        }

        public void Instrument(Action aspect, string instrumentationContext = null)
        {
            Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> contexts = null;

            try
            {
                if (PublishCounters)
                    contexts = BuildContexts();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (RaisePublishErrors)
                    throw;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                aspect();
            }
            finally
            {
                try
                {
                    if (PublishEvent)
                    {
                        InstrumentationEventSource.Instance.WriteInstrumentationEvent(_info.CategoryName,
                            _info.InstanceName, stopwatch.ElapsedMilliseconds, instrumentationContext);
                    }

                    if (PublishCounters)
                        CompleteContexts(contexts);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    if (RaisePublishErrors)
                        throw;
                }
            }           
          
        }

        public async Task InstrumentAsync(Func<Task> asyncAspect, string instrumentationContext = null)
        {
            Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> contexts = null;

            try
            {
                if (PublishCounters)
                    contexts = BuildContexts();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (RaisePublishErrors)
                    throw;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await asyncAspect();
            }
            finally
            {
                try
                {
                    if (PublishEvent)
                    {
                        InstrumentationEventSource.Instance.WriteInstrumentationEvent(_info.CategoryName,
                            _info.InstanceName, stopwatch.ElapsedMilliseconds, instrumentationContext);
                    }

                    if(PublishCounters)
                        CompleteContexts(contexts);
                }
                catch(Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    if(RaisePublishErrors)
                        throw;
                }
            }           
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
                try
                {
                    context.Handler.OnRequestStarting(ctx);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    if (RaisePublishErrors)
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

        /// <summary>
        /// Starts instrumentation
        /// </summary>
        /// <returns>The token to be passed to Finish method when finished</returns>
        public object Start()
        {
            return new InstrumentationToken()
            {
                Contexts = BuildContexts(),
                Kronometer = Stopwatch.StartNew()
            };
        }

        public void Finish(object token, string instrumentationContext = null)
        {
            var itoken = token as InstrumentationToken;
            if(itoken == null)
                throw new ArgumentException("This is an invalid token. Please pass the token provided when you you called Start(). Remember?", "token");

            if (PublishEvent)
            {
                InstrumentationEventSource.Instance.WriteInstrumentationEvent(_info.CategoryName,
                   _info.InstanceName, itoken.Kronometer.ElapsedMilliseconds, instrumentationContext);
            }

            CompleteContexts(itoken.Contexts);
        }

        private class InstrumentationToken
        {
            public Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> Contexts { get; set; }

            public Stopwatch Kronometer { get; set; }
        }
    }
}
