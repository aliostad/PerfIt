using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace PerfIt.Castle.Interception
{
    public class PerfItInterceptor : IInterceptor
    {
        private IInstanceNameProvider _instanceNameProvider;
        private IInstrumentationContextProvider _instrumentationContextProvider;
        private ConcurrentDictionary<string, SimpleInstrumentor> _instrumentors;
        private bool _inited;
        private readonly object _lock = new object();

        public PerfItInterceptor(string categoryName)
        {
            CategoryName = categoryName;
            PublishCounters = true;
            RaisePublishErrors = false;
            PublishEvent = true;
        }

        // TODO: TBD: decorate just methods? also properties? even fields? public? protected? private? internal?
        private static IInstrumentationInfo GetInstrumentationInfo(MemberInfo memberInfo)
        {
            // TODO: TBD: this pattern is found elsewhere in the Discoverer, so why aren't we potentially cross-referencing that resource?
            var attr = memberInfo.GetCustomAttributes<PerfItAttribute>(true).FirstOrDefault();
            // Returning Null is acceptable when no Decoration is discovered.
            return attr != null ? attr.Info : null;
        }

        private void Init()
        {
            _instrumentors = new ConcurrentDictionary<string, SimpleInstrumentor>();

            if (InstanceNameProviderType != null)
            {
                _instanceNameProvider = (IInstanceNameProvider) Activator.CreateInstance(
                    InstanceNameProviderType);
            }

            _instrumentationContextProvider = new InstrumentationContextProvider();

            if (InstrumentationContextProviderType != null)
            {
                _instrumentationContextProvider = (IInstrumentationContextProvider) Activator.CreateInstance(
                    InstrumentationContextProviderType);
            }

            _inited = true;
        }

        private ITwoStageInstrumentor InitInstrumentor(MethodInfo methodInfo)
        {
            // ReSharper disable once RedundantAssignment
            var instrumentationContext = string.Empty;
            var instrumentationInfo = GetInstrumentationInfo(methodInfo);

            if (instrumentationInfo == null) return null;

            var instanceName = instrumentationInfo.InstanceName;
            PublishCounters = instrumentationInfo.PublishCounters;

            if (string.IsNullOrEmpty(instanceName) && _instanceNameProvider != null)
                instanceName = _instanceNameProvider.GetInstanceName(methodInfo);

            if (string.IsNullOrEmpty(instanceName))
            {
                throw new InvalidOperationException(
                    "Either InstanceName or InstanceNameProviderType must be supplied.");
            }

            if (_instrumentationContextProvider != null)
            {
                instrumentationContext = _instrumentationContextProvider.GetContext(methodInfo);
            }
            else
            {
                throw new InvalidOperationException(
                    "The Instrumentation Context Cannot be Null. Define a InstrumentationContextProvider implementation.");
            }

            SetEventPolicy();
            SetPublishCounterPolicy();
            SetErrorPolicy();

            var e = new InstrumentorRequiredEventArgs(CategoryName, instrumentationInfo);
            RaiseInstrumentorRequired(e);

            _instrumentors.AddOrUpdate(instrumentationContext, e.Instrumentor, (ictx, i) => e.Instrumentor);

            return e.Instrumentor;
        }

        /// <summary>
        /// InstrumentorRequired event.
        /// </summary>
        public event EventHandler<InstrumentorRequiredEventArgs> InstrumentorRequired;

        private void RaiseInstrumentorRequired(InstrumentorRequiredEventArgs e)
        {
            if (InstrumentorRequired == null) return;
            InstrumentorRequired(this, e);
        }

        public void Intercept(IInvocation invocation)
        {
            if (!(PublishCounters || PublishEvent))
            {
                invocation.Proceed();
            }
            else
            {
                try
                {
                    var instrumentationContext = string.Empty;

                    if (_instrumentationContextProvider != null)
                    {
                        instrumentationContext = _instrumentationContextProvider.GetContext(
                            invocation.MethodInvocationTarget);
                    }

                    // TODO: TBD: was: if not inited then lock if not initted then init? huh?
                    lock (_lock)
                    {
                        if (!_inited)
                        {
                            Init();
                        }
                    }

                    SimpleInstrumentor instrumentor;

                    if (_instrumentors == null
                        || !_instrumentors.TryGetValue(instrumentationContext, out instrumentor))
                    {
                        instrumentor = (SimpleInstrumentor) InitInstrumentor(
                            invocation.MethodInvocationTarget);
                    }

                    var returnType = invocation.Method.ReturnType;

                    if (returnType != typeof(void)
                        && (returnType == typeof(Task)
                            || (returnType.IsGenericType
                                && returnType.GetGenericTypeDefinition() == typeof(Task<>))))
                    {
                        // ProceedAsync is syntactic sugar, leveraging extension methods.
                        instrumentor.InstrumentAsync(invocation.ProceedAsync, instrumentationContext, SamplingRate).Wait();
                    }
                    else
                    {
                        instrumentor.Instrument(invocation.Proceed, instrumentationContext, SamplingRate);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    if (RaisePublishErrors) throw;
                }
            }
        }

        private void SetPublishCounterPolicy()
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

        public Type InstanceNameProviderType { get; set; }

        public Type InstrumentationContextProviderType { get; set; }

        public double SamplingRate { get; set; }

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

        public string CategoryName { get; set; }
    }
}
