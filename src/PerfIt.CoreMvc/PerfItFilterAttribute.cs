using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.CoreMvc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PerfItFilterAttribute : ActionFilterAttribute, IInstrumentationInfo
    {
        public PerfItFilterAttribute(string categoryName)
        {
            CategoryName = categoryName;
        }

        private ITwoStageInstrumentor _instrumentor;
        private IInstanceNameProvider _instanceNameProvider = null;
        private IInstrumentationContextProvider _instrumentationContextProvider = null;
        private const string PerfItTwoStageKey = "__#_PerfItTwoStageKeyCoreMvc_#__";
        private bool _inited = false;
        private object _lock = new object();

        private void Init(ActionExecutingContext actionContext)
        {
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

            _instanceNameProvider = _instanceNameProvider ?? 
                (IInstanceNameProvider) actionContext.HttpContext.RequestServices.GetService(typeof(IInstanceNameProvider));

            if (InstrumentationContextProviderType != null)
            {
                _instrumentationContextProvider = (IInstrumentationContextProvider)Activator.CreateInstance(InstrumentationContextProviderType);
            }         

            _instrumentationContextProvider = _instrumentationContextProvider ??
                (IInstrumentationContextProvider) actionContext.HttpContext.RequestServices.GetService(typeof(IInstrumentationContextProvider));

            var instanceName = InstanceName;
            if (_instanceNameProvider != null)
                instanceName = _instanceNameProvider.GetInstanceName(actionContext);

            if (instanceName == null)
                instanceName =
                    PerfItRuntime.GetCounterInstanceName(actionContext.Controller.GetType(),
                        actionContext.ActionDescriptor.DisplayName);

            var inst = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = Description,
                InstanceName = instanceName,
                CategoryName = CategoryName,
                SamplingRate = SamplingRate,
                RaisePublishErrors = RaisePublishErrors
            });

            _instrumentor = inst;  
        }
        
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            try
            {
                var instrumentationContext = new InstrumentationContext
                {
                    Text1 = string.Format("{0}_{1}", context.ActionDescriptor.DisplayName,
                        context.HttpContext.Request.Path)
                };

                if (_instrumentationContextProvider != null)
                    instrumentationContext = _instrumentationContextProvider.GetContext(context);

                if (context.HttpContext.Items.ContainsKey(PerfItTwoStageKey))
                {
                    var token = context.HttpContext.Items[PerfItTwoStageKey] as InstrumentationToken;
                    if (context.Exception != null && token != null)
                    {
                        token.Contexts.SetContextToErrorState();
                    }

                    _instrumentor.Finish(token, instrumentationContext);
                }
            }
            catch (Exception exception)
            {
                if (RaisePublishErrors)
                    throw;
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (!_inited)
            {
                lock (_lock)
                {
                    if (!_inited)
                    {
                        Init(context);
                        _inited = true;
                    }
                }
            }

            var token = _instrumentor.Start(SamplingRate);
            context.HttpContext.Items[PerfItTwoStageKey] = token;
        }

        public string InstanceName { get; set; }

        public string Description { get; set; }

        public string CategoryName { get; set; }

        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

        public double SamplingRate { get; set; }

        public string CorrelationIdKey { get; set; }

        /// <summary>
        /// Optional. A type implementing IInstanceNameProvider. If provided, it will be used to drive the instance name.
        /// </summary>
        public Type InstanceNameProviderType { get; set; }

        /// <summary>
        /// Optional. A type implementing IInstrumentationContextProvider. If provided, it will be used to drive the context gathering at instrumentation time.
        /// </summary>
        public Type InstrumentationContextProviderType { get; set; }

        protected void SetErrorPolicy()
        {
            RaisePublishErrors = PerfItRuntime.IsPublishErrorsEnabled(CategoryName, RaisePublishErrors);
        }

        protected void SetSamplingRate()
        {
            SamplingRate = PerfItRuntime.GetSamplingRate(CategoryName, SamplingRate);
        }
    }
}
