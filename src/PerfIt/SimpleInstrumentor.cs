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

        private readonly Dictionary<string, ITwoStageTracer> _tracers = new Dictionary<string, ITwoStageTracer>();

        public SimpleInstrumentor(IInstrumentationInfo info)
        {
            _info = info;
            _info.CorrelationIdKey = _info.CorrelationIdKey ?? Correlation.CorrelationIdKey;
            _tracers.Add("EventSourceTracer", new EventSourceTracer());
#if NET452
            if (_info.PublishCounters)
            {
                _tracers.Add("PerformanceCounterTracer", new PerformanceCounterTracer(info));
            }
#endif
        }

        bool ShouldInstrument(double samplingRate)
        {
            var corrId = Correlation.GetId(_info.CorrelationIdKey);
            return ShouldInstrument(samplingRate, corrId.ToString());
        }

        internal static bool ShouldInstrument(double samplingRate, string corrId)
        {
            var d = Math.Abs(corrId.GetHashCode() * 1.0) / Math.Abs(int.MaxValue * 1.0);
            return d < samplingRate;
        }

        /// <summary>
        /// Not thread-safe. It should be populated only at the time of initialisation
        /// </summary>
        public IDictionary<string, ITwoStageTracer> Tracers
        {
            get
            {
                return _tracers;
            }
        }

        public void Instrument(Action aspect,
            double? samplingRate = null, InstrumentationContext extraContext = null)
        {
            var token = Start(samplingRate ?? _info.SamplingRate);
            try
            {
                aspect();
            }            
            finally
            {
                Finish(token, extraContext);
            }                    
        }

        public async Task InstrumentAsync(Func<Task> asyncAspect, 
            double? samplingRate = null, InstrumentationContext extraContext = null)
        {
            var token = Start(samplingRate ?? _info.SamplingRate);
            try
            {
                await asyncAspect();
            }
            finally
            {
                Finish(token, extraContext);
            }
        }

        private Dictionary<string, object> BuildContexts()
        {
            var ctx = new Dictionary<string, object>();
            ctx.Add(Constants.PerfItKey, new PerfItContext());
            ctx.Add(Constants.PerfItPublishErrorsKey, _info.RaisePublishErrors);
            return ctx;
        }

        /// <summary>
        /// Starts instrumentation
        /// </summary>
        /// <returns>The token to be passed to Finish method when finished</returns>
        public object Start(double samplingRate = Constants.DefaultSamplingRate)
        {
            try
            {
                var token = new InstrumentationToken()
                {
                    Contexts = BuildContexts(),
                    Kronometer = Stopwatch.StartNew(),
                    SamplingRate = samplingRate,
                    CorrelationId = Correlation.GetId(_info.CorrelationIdKey),
                    TracerContexts = new Dictionary<string, object>()
                };

                foreach (var kv in _tracers)
                {
                    token.TracerContexts.Add(kv.Key, kv.Value.Start(_info));
                }

                return token;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                if(_info.RaisePublishErrors)
                    throw;
            }

            return null;
        }

        public void Finish(object token, InstrumentationContext extraContext = null)
        {
            if(token == null)
                return; // not meant to be instrumented prob due to sampling rate

            try
            {
                var itoken = ValidateToken(token);

                if (ShouldInstrument(itoken.SamplingRate))
                {
                    foreach (var kv in _tracers)
                    {
                        kv.Value.Finish(itoken.TracerContexts[kv.Key], itoken.Kronometer.ElapsedMilliseconds,
                            itoken.CorrelationId?.ToString(),
                            extraContext);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if(_info.RaisePublishErrors)
                    throw;
            }
        }
        
        private static InstrumentationToken ValidateToken(object token)
        {
            var itoken = token as InstrumentationToken;
            if (itoken == null)
                throw new ArgumentException(
                    "This is an invalid token. Please pass the token provided when you you called Start(). Remember?", "token");
            return itoken;
        }

        public void Dispose()
        {
            foreach (var tracer in _tracers.Values)
            {
                tracer.Dispose();
            }
        }
    }
}
