using System;
using System.Diagnostics;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PerfIt.WebApi
{
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

        /// <summary>
        /// Optional InstanceName of the Counter. If not specified it will be [Controller].[Action]
        /// for each Counter. If it is provided, make sure it is Unique within the project
        /// </summary>
        public string InstanceName
        {
            get { return Info.InstanceName; }
            set { Info.InstanceName = value; }
        }

        /// <summary>
        /// Description of the counter. Will be published to counter metadata visible in Perfmon.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the Counters. Each value as a string.
        /// </summary>
        public string[] Counters
        {
            get { return Info.Counters; }
            set { Info.Counters = value; }
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

        public string CategoryName
        {
            get { return Info.CategoryName; }
            set { Info.CategoryName = value; }
        }

        public double SamplingRate
        {
            get { return Info.SamplingRate; }
            set { Info.SamplingRate = value; }
        }

        private void Init(HttpActionContext actionContext)
        {
            SetEventPolicy();
            SetPublishCounterPolicy();
            SetErrorPolicy();

            if (InstanceNameProviderType != null)
            {
                _instanceNameProvider = (IInstanceNameProvider) Activator.CreateInstance(
                    InstanceNameProviderType);
            }

            if (InstrumentationContextProviderType != null)
            {
                _instrumentationContextProvider = (IInstrumentationContextProvider) Activator.CreateInstance(
                    InstrumentationContextProviderType);
            }

            var instanceName = InstanceName;

            if (_instanceNameProvider != null)
            {
                InstanceName = _instanceNameProvider.GetInstanceName(actionContext);
            }

            if (instanceName == null)
            {
                InstanceName = PerfItRuntime.GetCounterInstanceName(
                    actionContext.ControllerContext.ControllerDescriptor.ControllerType,
                    actionContext.ActionDescriptor.ActionName);
            }

            _instrumentor = new SimpleInstrumentor(Info);
        }

        /// <summary>
        /// Optional. A type implementing IInstanceNameProvider. If provided, it will be used to drive the instance name.
        /// </summary>
        public Type InstanceNameProviderType { get; set; }

         /// <summary>
        /// Optional. A type implementing IInstrumentationContextProvider. If provided, it will be used to drive the context gathering at instrumentation time.
        /// </summary>
        public Type InstrumentationContextProviderType { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
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
            actionContext.Request.Properties.Add(PerfItTwoStageKey, token);
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

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {

            base.OnActionExecuted(actionExecutedContext);

            try
            {
                var instrumentationContext = string.Format("{0}_{1}", actionExecutedContext.Request.Method,
                    actionExecutedContext.Request.RequestUri);

                if (_instrumentationContextProvider != null)
                    instrumentationContext = _instrumentationContextProvider.GetContext(actionExecutedContext);

                if (!actionExecutedContext.Request.Properties.ContainsKey(PerfItTwoStageKey)) return;

                var token = actionExecutedContext.Request.Properties[PerfItTwoStageKey] as InstrumentationToken;

                if (!(actionExecutedContext.Exception == null || token == null))
                {
                    token.Context.Data.SetContextToErrorState();
                }

                _instrumentor.Finish(token, instrumentationContext);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                if (RaisePublishErrors) throw;
            }
        }

        protected override string ProvideInstrumentationContext(HttpActionExecutedContext actionExecutedContext)
        {
            return string.Join("#",
                actionExecutedContext.Response == null ? "NoResponse" : actionExecutedContext.Response.StatusCode.ToString(),
                actionExecutedContext.Request == null ? "NoRequest" : actionExecutedContext.Request.RequestUri.AbsoluteUri
            );
        }
    }
}
