using System;
using System.Diagnostics;
using System.Web.Mvc;

namespace PerfIt.Mvc
{
    // TODO: TBD: ditto WebApi, but for subtle Mvc nuances...
    public class PerfItFilterAttribute : InstrumentationContextProviderBaseAttribute
    {
        public IInstrumentationInfo Info { get; private set; }

        private ITwoStageInstrumentor _instrumentor;
        private IInstanceNameProvider _instanceNameProvider;
        private IInstrumentationContextProvider _instrumentationContextProvider;
        private const string PerfItTwoStageKey = "__#_PerfItTwoStageKey_#__";
        private bool _inited;
        private readonly object _lock = new object();

        public PerfItFilterAttribute(string categoryName)
        {
            Info = new InstrumentationInfo {CategoryName = categoryName};
        }

        private void Init(ActionExecutingContext actionContext)
        {
            SetEventPolicy();
            SetPublishCounterPolicy();
            SetErrorPolicy();

            if (InstanceNameProviderType != null)
            {
                _instanceNameProvider = (IInstanceNameProvider)Activator.CreateInstance(InstanceNameProviderType);
            }

            if (InstrumentationContextProviderType != null)
            {
                _instrumentationContextProvider = (IInstrumentationContextProvider)Activator.CreateInstance(InstrumentationContextProviderType);
            }

            var instanceName = InstanceName;

            if (_instanceNameProvider != null)
                instanceName = _instanceNameProvider.GetInstanceName(actionContext);

            if (instanceName == null)
            {
                InstanceName = PerfItRuntime.GetCounterInstanceName(
                    actionContext.ActionDescriptor.ControllerDescriptor.ControllerType,
                    actionContext.ActionDescriptor.ActionName);
            }

            _instrumentor = new SimpleInstrumentor(Info);
        }

        public string InstanceName
        {
            get { return Info.InstanceName; }
            set { Info.InstanceName = value; }
        }

        public string Description
        {
            get { return Info.Description; }
            set { Info.Description = value; }
        }

        public string[] Counters
        {
            get { return Info.Counters; }
            set { Info.Counters = value; }
        }

        public string CategoryName
        {
            get { return Info.CategoryName; }
            set { Info.CategoryName = value; }
        }

        public bool PublishCounters
        {
            get { return Info.PublishCounters; }
            set { Info.PublishCounters = value; }
        }

        public bool RaisePublishErrors
        {
            get { return Info.RaisePublishErrors; }
            set { Info.RaisePublishErrors = value; }
        }

        public bool PublishEvent
        {
            get { return Info.PublishEvent; }
            set { Info.PublishEvent = value; }
        }

        public double SamplingRate
        {
            get { return Info.SamplingRate; }
            set { Info.SamplingRate = value; }
        }

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

        protected void SetEventPolicy()
        {
            PublishEvent = PerfItRuntime.IsPublishEventsEnabled(CategoryName, PublishEvent);
        }

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            try
            {
                var instrumentationContext = string.Format("{0}_{1}", actionExecutedContext.ActionDescriptor.ActionName,
                    actionExecutedContext.HttpContext.Request.Url);

                if (_instrumentationContextProvider != null)
                    instrumentationContext = _instrumentationContextProvider.GetContext(actionExecutedContext);

                if (!actionExecutedContext.HttpContext.Items.Contains(PerfItTwoStageKey)) return;

                var token = actionExecutedContext.HttpContext.Items[PerfItTwoStageKey] as InstrumentationToken;

                if (!(actionExecutedContext.Exception == null || token == null))
                {
                    token.Context.Data.SetContextToErrorState();
                }

                _instrumentor.Finish(token, instrumentationContext);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                if (RaisePublishErrors)
                    throw;
            }
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            lock (_lock)
            {
                if (!_inited)
                {
                    Init(actionContext);
                    _inited = true;
                }
            }

            if (!(PublishCounters || PublishEvent)) return;

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
