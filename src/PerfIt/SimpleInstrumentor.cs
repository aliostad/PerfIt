using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PerfIt
{
    /// <summary>
    /// SimpleInstrumentor implementation.
    /// </summary>
    public class SimpleInstrumentor : IInstrumentor, ITwoStageInstrumentor
    {
        private readonly IInstrumentationInfo _info;

        private readonly ConcurrentDictionary<string, Lazy<PerfitHandlerContext>> _counterContexts
            = new ConcurrentDictionary<string, Lazy<PerfitHandlerContext>>();

        private readonly Random _random = new Random();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info"></param>
        public SimpleInstrumentor(IInstrumentationInfo info)
        {
            _info = info;

            PublishInstrumentationCallback = InstrumentationEventSource.Instance.WriteInstrumentationEvent;
        }

        /// <summary>
        /// Returns whether ShouldInstrument the invocation. <paramref name="samplingRate"/>
        /// helps to throttle how frequent a report is made to the Performance Monitor.
        /// </summary>
        /// <param name="samplingRate">Throttles the rate of sampled counts.</param>
        /// <returns></returns>
        private bool ShouldInstrument(double samplingRate)
        {
            return _random.NextDouble() <= samplingRate;
        }

        /// <summary>
        /// Instruments the <paramref name="aspect"/> given
        /// <paramref name="instrumentationContext"/> and <paramref name="samplingRate"/>.
        /// </summary>
        /// <param name="aspect"></param>
        /// <param name="instrumentationContext"></param>
        /// <param name="samplingRate"></param>
        /// <see cref="Constants.DefaultSamplingRate"/>
        public void Instrument(Action aspect, string instrumentationContext = null,
            double samplingRate = Constants.DefaultSamplingRate)
        {
            try
            {
                using (var context = GetInstrumentationContext())
                {
                    try
                    {
                        aspect();
                    }
                    catch (Exception)
                    {
                        context.Data.SetContextToErrorState();
                        throw;
                    }
                    finally
                    {
                        try
                        {
                            if (_info.PublishEvent && ShouldInstrument(samplingRate))
                            {
                                var elapsed = context.Stopwatch.Elapsed;
                                PublishInstrumentationCallback(_info.CategoryName, _info.InstanceName,
                                    elapsed.TotalMilliseconds, instrumentationContext);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.ToString());
                            if (_info.RaisePublishErrors) throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                if (_info.RaisePublishErrors) throw;
            }
        }

        /// <summary>
        /// PublishInstrumentationCallback delegate instance.
        /// </summary>
        public PublishInstrumentationDelegate PublishInstrumentationCallback { get; set; }

        /// <summary>
        /// Instruments the <paramref name="asyncAspect"/> given
        /// <paramref name="instrumentationContext"/> and <paramref name="samplingRate"/>.
        /// </summary>
        /// <param name="asyncAspect"></param>
        /// <param name="instrumentationContext"></param>
        /// <param name="samplingRate"></param>
        /// <returns></returns>
        /// <see cref="Constants.DefaultSamplingRate"/>
        public async Task InstrumentAsync(Func<Task> asyncAspect, string instrumentationContext = null,
            double samplingRate = Constants.DefaultSamplingRate)
        {
            using (var context = GetInstrumentationContext())
            {
                try
                {
                    asyncAspect().Wait();
                }
                catch (Exception)
                {
                    context.Data.SetContextToErrorState();
                    throw;
                }
                finally
                {
                    try
                    {
                        if (_info.PublishEvent && ShouldInstrument(samplingRate))
                        {
                            var elapsed = context.Stopwatch.Elapsed;
                            PublishInstrumentationCallback(_info.CategoryName, _info.InstanceName,
                                elapsed.TotalMilliseconds, instrumentationContext);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                        if (_info.RaisePublishErrors) throw;
                    }
                }
            }
        }

        private InstrumentationContext GetInstrumentationContext()
        {
            try
            {
                var contexts = GetContexts().ToArray();

                var data = new Dictionary<string, object>
                {
                    {Constants.PerfItKey, new PerfItContext()},
                    {Constants.PerfItPublishErrorsKey, _info.RaisePublishErrors}
                };

                foreach (var context in contexts)
                {
                    try
                    {
                        context.Handler.OnRequestStarting(data);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                        if (_info.RaisePublishErrors)
                            throw;
                    }
                }

                return new InstrumentationContext(data, contexts);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                if (_info.RaisePublishErrors) throw;
            }

            return null;
        }

        private IEnumerable<PerfitHandlerContext> GetContexts()
        {
            // TODO: TBD: runtime cross cutting concern... inject factories concern into instrumentor via ctor
            foreach (var handlerFactory in PerfItRuntime.HandlerFactories)
            {
                var hf = handlerFactory;

                var key = GetKey(hf.Key, _info.InstanceName);

                var ctx = _counterContexts.GetOrAdd(key, k =>
                    new Lazy<PerfitHandlerContext>(() => new PerfitHandlerContext
                    {
                        Handler = hf.Value(_info.CategoryName, _info.InstanceName),
                        Name = _info.InstanceName
                    }));

                yield return ctx.Value;
            }
        }

        private static string GetKey(string counterName, string instanceName)
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
            return new InstrumentationToken
            {
                Context = _info.RequiresInstrumentationContext ? GetInstrumentationContext() : null,
                SamplingRate = samplingRate
            };
        }

        /// <summary>
        /// Finishes the instrumentation.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="instrumentationContext"></param>
        public void Finish(object token, string instrumentationContext = null)
        {
            if (token == null)
                return; // not meant to be instrumented prob due to sampling rate

            var itoken = ValidateToken(token);

            if (_info.PublishEvent && ShouldInstrument(itoken.SamplingRate))
            {
                PublishInstrumentationCallback(_info.CategoryName, _info.InstanceName,
                    itoken.Context.Stopwatch.ElapsedMilliseconds, instrumentationContext);
            }

            if (_info.RequiresInstrumentationContext)
                itoken.Context.Dispose();
        }

        private static InstrumentationToken ValidateToken(object token)
        {
            var itoken = token as InstrumentationToken;
            if (itoken == null)
                throw new ArgumentException(
                    "This is an invalid token. Please pass the token provided when you you called Start(). Remember?",
                    "token");
            return itoken;
        }
    }
}
