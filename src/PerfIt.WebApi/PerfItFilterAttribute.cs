using System;
using System.Configuration;
using System.Diagnostics;
using System.Security.Principal;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PerfIt.WebApi
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
            PublishEvent = true;
            CategoryName = categoryName;
        }

        private void Init(HttpActionContext actionContext)
        {
            SetEventPolicy();
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
                    PerfItRuntime.GetCounterInstanceName(actionContext.ControllerContext.ControllerDescriptor.ControllerType,
                        actionContext.ActionDescriptor.ActionName);

            var inst = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = Description,
                Counters = Counters,
                InstanceName =  instanceName,
                CategoryName = CategoryName,
                SamplingRate = SamplingRate,
                PublishCounters = PublishCounters,
                PublishEvent = PublishEvent,
                RaisePublishErrors = RaisePublishErrors
            });

            _instrumentor = inst;

            if (TracerTypes!=null)
            {
                foreach (var tt in TracerTypes)
                {
                    inst.Tracers.Add(tt.FullName, (ITwoStageTracer)Activator.CreateInstance(tt));
                }
            }
        }

        /// <summary>
        /// Optional name of the counter. 
        /// If not specified it will be [controller].[action] for each counter.
        /// If it is provided, make sure it is UNIQUE within the project
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// Description of the counter. Will be published to counter metadata visible in Perfmon.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Counter types. Each value as a string.
        /// </summary>
        public string[] Counters { get; set; }

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

        public string CategoryName { get; set; }

        public double SamplingRate { get; set; }

        /// <summary>
        /// Implementations of ITwoStageTracer interface. Optional.
        /// </summary>
        public Type[] TracerTypes { get; set; }

        public string CorrelationIdKey { get; set; }

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

            if (PublishCounters || PublishEvent)
            {
                var token = _instrumentor.Start(SamplingRate);
                actionContext.Request.Properties.Add(PerfItTwoStageKey, token);
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

        protected void SetSamplingRate()
        {
            SamplingRate = PerfItRuntime.GetSamplingRate(CategoryName, SamplingRate);
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

                if (actionExecutedContext.Request.Properties.ContainsKey(PerfItTwoStageKey))
                {
                    var token = actionExecutedContext.Request.Properties[PerfItTwoStageKey] as InstrumentationToken;
                    if (actionExecutedContext.Exception != null && token != null)
                    {
                        token.Contexts.Item2.SetContextToErrorState();
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

        protected override string ProvideInstrumentationContext(HttpActionExecutedContext actionExecutedContext)
        {
            return string.Join("#",
                actionExecutedContext.Response == null ? "NoResponse" : actionExecutedContext.Response.StatusCode.ToString(),
                actionExecutedContext.Request == null ? "NoRequest" : actionExecutedContext.Request.RequestUri.AbsoluteUri
            );
        }
    }
}
