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
            SetPublish();
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

            if (string.IsNullOrEmpty(instanceName))
            {
                throw new InvalidOperationException("Either InstanceName or InstanceNameProviderType must be supplied.");
            }

            _instrumentor = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = Description,
                Counters = Counters,
                InstanceName = InstanceName,
                CategoryName = CategoryName
            }, PublishCounters, PublishEvent, RaisePublishErrors);

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

        public Type InstanceNameProviderType { get; set; }

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

            if (PublishCounters)
            {
                var token = _instrumentor.Start();
                actionContext.Request.Properties.Add(PerfItTwoStageKey, token);
            }
        }

        private void SetPublish()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishCounters] ?? PublishCounters.ToString();
            PublishCounters = Convert.ToBoolean(value);
        }

        protected void SetErrorPolicy()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishErrors] ?? RaisePublishErrors.ToString();
            RaisePublishErrors = Convert.ToBoolean(value);
        }

        protected void SetEventPolicy()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishEvent] ?? PublishEvent.ToString();
            PublishEvent = Convert.ToBoolean(value);
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
                    var token = actionExecutedContext.Request.Properties[PerfItTwoStageKey];
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
                actionExecutedContext.Response.StatusCode.ToString(),
                actionExecutedContext.Request.RequestUri.AbsoluteUri
            );
        }
    }
}
