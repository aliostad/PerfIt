using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace PerfIt.Mvc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PerfItFilterAttribute : InstrumentationContextProviderBaseAttribute, IInstrumentationInfo
    {
        private ITwoStageInstrumentor _instrumentor;
        private IInstanceNameProvider _instanceNameProvider = null;
        private IInstrumentationContextProvider _instrumentationContextProvider = null;
        private const string PerfItTwoStageKey = "__#_PerfItTwoStageKey_#__";
        private bool _inited = false;
        private object _lock = new object();

        public PerfItFilterAttribute(string categoryName)
        {
            Description = string.Empty;
            PublishCounters = true;
            RaisePublishErrors = false;
            CategoryName = categoryName;
            
        }

        private void Init(ActionExecutingContext actionContext)
        {
            SetPublishCounterPolicy();
            SetErrorPolicy();
            SetSamplingRate();

            if (SamplingRate == default(double))
            {
                SamplingRate = Constants.DefaultSamplingRate;
            }

            if (InstanceNameProviderType != null)
            {
                _instanceNameProvider = (IInstanceNameProvider)Activator.CreateInstance(InstanceNameProviderType);
            }

            if (InstrumentationContextProviderType != null)
            {
                _instrumentationContextProvider = (IInstrumentationContextProvider)Activator.CreateInstance(InstrumentationContextProviderType);
            }

            if (Counters == null || Counters.Length == 0)
            {
                Counters = CounterTypes.StandardCounters;
            }

            var instanceName = InstanceName;
            if (_instanceNameProvider != null)
                instanceName = _instanceNameProvider.GetInstanceName(actionContext);

            if (instanceName == null)
                instanceName =
                    PerfItRuntime.GetCounterInstanceName(actionContext.ActionDescriptor.ControllerDescriptor.ControllerType,
                        actionContext.ActionDescriptor.ActionName);

            var inst = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = Description,
                Counters = Counters,
                InstanceName = instanceName,
                CategoryName = CategoryName,
                SamplingRate = SamplingRate,
                PublishCounters = PublishCounters,
                RaisePublishErrors = RaisePublishErrors
            });

            _instrumentor = inst;

            if (TracerTypes != null)
            {
                foreach (var tt in TracerTypes)
                {
                    inst.Tracers.Add(tt.FullName, (ITwoStageTracer)Activator.CreateInstance(tt));
                }
            }
        }


        public string InstanceName { get; set; }

        public string Description { get; set; }

        public string[] Counters { get; set; }

        public string CategoryName { get; set; }

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        public double SamplingRate { get; set; }

        public string CorrelationIdKey { get; set; }

        /// <summary>
        /// Implementations of ITwoStageTracer interface. Optional.
        /// </summary>
        public Type[] TracerTypes { get; set; }

        /// <summary>
        /// Optional. A type implementing IInstanceNameProvider. If provided, it will be used to drive the instance name.
        /// </summary>
        public Type InstanceNameProviderType { get; set; }

        /// <summary>
        /// Optional. A type implementing IInstrumentationContextProvider. If provided, it will be used to drive the context gathering at instrumentation time.
        /// </summary>
        public Type InstrumentationContextProviderType { get; set; }

        private void SetPublishCounterPolicy()
        {
            PublishCounters = PerfItRuntime.IsPublishCounterEnabled(CategoryName, PublishCounters);
        }

        protected void SetErrorPolicy()
        {
            RaisePublishErrors = PerfItRuntime.IsPublishErrorsEnabled(CategoryName, RaisePublishErrors);
        }

        protected void SetSamplingRate()
        {
            SamplingRate = PerfItRuntime.GetSamplingRate(CategoryName, SamplingRate);
        }

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            try
            {
                var instrumentationContext = new InstrumentationContext
                {
                    Text1 = string.Format("{0}_{1}", actionExecutedContext.ActionDescriptor.ActionName,
                        actionExecutedContext.HttpContext.Request.Url)
                };

                if (_instrumentationContextProvider != null)
                    instrumentationContext = _instrumentationContextProvider.GetContext(actionExecutedContext);

                if (actionExecutedContext.HttpContext.Items.Contains(PerfItTwoStageKey))
                {
                    var token = actionExecutedContext.HttpContext.Items[PerfItTwoStageKey] as InstrumentationToken;
                    if (actionExecutedContext.Exception != null && token != null)
                    {
                        token.Contexts.SetContextToErrorState();
                    }

                    _instrumentor.Finish(token, instrumentationContext);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
                if (RaisePublishErrors)
                    throw;
            }
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            if (!_inited)
            {
                lock (_lock)
                {
                    if (!_inited)
                    {
                        Init(actionContext);
                        _inited = true;
                    }
                }
            }

            var token = _instrumentor.Start(SamplingRate);
            actionContext.HttpContext.Items[PerfItTwoStageKey] = token;
        }

        protected override string ProvideInstrumentationContext(ActionExecutedContext actionExecutedContext)
        {
            return string.Join("#",
                actionExecutedContext.HttpContext.Response == null ? "NoResponse" : actionExecutedContext.HttpContext.Response.StatusCode.ToString(),
                actionExecutedContext.HttpContext.Request == null ? "NoRequest" : actionExecutedContext.HttpContext.Request.Url.AbsoluteUri
            );
        }
    }
}
