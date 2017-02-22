using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PerfIt
{
    public class SimpleInstrumentor : IInstrumentor, ITwoStageInstrumentor, IDisposable
    {
        private IInstrumentationInfo _info;

        private ConcurrentDictionary<string, Lazy<PerfitHandlerContext>> _counterContexts =
          new ConcurrentDictionary<string, Lazy<PerfitHandlerContext>>();
        private Random _random = new Random();
        private string _correlationIdKey;

        public SimpleInstrumentor(IInstrumentationInfo info, string correlationIdKey = Correlation.CorrelationIdKey)
        {
            _correlationIdKey = correlationIdKey;
            _info = info;
            PublishInstrumentationCallback = InstrumentationEventSource.Instance.WriteInstrumentationEvent;
        }

        private bool ShouldInstrument(double samplingRate)
        {
            return _random.NextDouble() < samplingRate;
        }

        public void Instrument(Action aspect, string instrumentationContext = null, double samplingRate = Constants.DefaultSamplingRate)
        {
            Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> contexts = null;
            var corrId = Correlation.GetId(_correlationIdKey);
            try
            {
                if (_info.PublishCounters)
                    contexts = BuildContexts();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (_info.RaisePublishErrors)
                    throw;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                aspect();
            }
            catch (Exception)
            {
                SetErrorContexts(contexts);
                throw;
            }
            finally
            {
                try
                {
                    if (_info.PublishEvent && ShouldInstrument(samplingRate))
                    {
                        PublishInstrumentationCallback(_info.CategoryName,
                            _info.InstanceName, stopwatch.ElapsedMilliseconds, instrumentationContext, corrId);
                    }

                    if (_info.PublishCounters)
                        CompleteContexts(contexts);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    if (_info.RaisePublishErrors)
                        throw;
                }
            }           
          
        }

        public Action<string, string, long, string, string> PublishInstrumentationCallback { get; set; }

        private void SetErrorContexts(Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> contexts)
        {
            if (contexts != null && contexts.Item2 != null)
            {
                contexts.Item2.SetContextToErrorState();
            }
        }

        public async Task InstrumentAsync(Func<Task> asyncAspect, string instrumentationContext = null, double samplingRate = Constants.DefaultSamplingRate)
        {
            Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> contexts = null;
            var corrId = Correlation.GetId(_correlationIdKey);

            try
            {
                if (_info.PublishCounters)
                    contexts = BuildContexts();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (_info.RaisePublishErrors)
                    throw;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await asyncAspect();
            }
            catch (Exception)
            {
                SetErrorContexts(contexts);
                throw;
            }
            finally
            {
                try
                {
                    if (_info.PublishEvent && ShouldInstrument(samplingRate))
                    {
                        PublishInstrumentationCallback(_info.CategoryName,
                            _info.InstanceName, stopwatch.ElapsedMilliseconds, instrumentationContext, corrId);
                    }

                    if (_info.PublishCounters)
                        CompleteContexts(contexts);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    if (_info.RaisePublishErrors)
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
            var counters = _info.Counters==null || _info.Counters.Length == 0 ? CounterTypes.StandardCounters : _info.Counters;

            foreach (var handlerFactory in PerfItRuntime.HandlerFactories.Where(c=> counters.Contains(c.Key)))
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

        /// <summary>
        /// Starts instrumentation
        /// </summary>
        /// <returns>The token to be passed to Finish method when finished</returns>
        public object Start(double samplingRate = Constants.DefaultSamplingRate)
        {
            return new InstrumentationToken()
            {
                Contexts = _info.PublishCounters ? BuildContexts() : null,
                Kronometer = Stopwatch.StartNew(),
                SamplingRate = samplingRate,
                CorrelationId = Correlation.GetId()
            };
        }

        public void Finish(object token, string instrumentationContext = null)
        {
            if(token == null)
                return; // not meant to be instrumented prob due to sampling rate

            var itoken = ValidateToken(token);

            if (_info.PublishEvent && ShouldInstrument(itoken.SamplingRate))
            {
                PublishInstrumentationCallback(_info.CategoryName,
                   _info.InstanceName, itoken.Kronometer.ElapsedMilliseconds, instrumentationContext, itoken.CorrelationId);
            }

            if (_info.PublishCounters)
                CompleteContexts(itoken.Contexts);
        }
        
        private static InstrumentationToken ValidateToken(object token)
        {
            var itoken = token as InstrumentationToken;
            if (itoken == null)
                throw new ArgumentException(
                    "This is an invalid token. Please pass the token provided when you you called Start(). Remember?", "token");
            return itoken;
        }
    }
}
