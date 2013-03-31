using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public abstract class CounterHandlerBase : ICounterHandler
    {
        protected string _applicationName;
        protected PerfItFilterAttribute _filter;

        public CounterHandlerBase(string applicationName, PerfItFilterAttribute filter)
        {
            _filter = filter;
            CounterName = filter.CategoryName;
            _applicationName = applicationName;
        }

        public virtual void Dispose()
        {
            
        }


        public abstract string CounterType { get; }
        protected abstract void OnRequestStarting(HttpRequestMessage request, PerfItContext context);
        protected abstract void OnRequestEnding(HttpResponseMessage response, PerfItContext context);
        protected abstract CounterCreationData[] DoGetCreationData(PerfItFilterAttribute filter);

        public void OnRequestStarting(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(Constants.PerfItKey))
            {
                OnRequestStarting(request, (PerfItContext) request.Properties[Constants.PerfItKey]);
            }
        }

        public void OnRequestEnding(HttpResponseMessage response)
        {
            if (response.RequestMessage.Properties.ContainsKey(Constants.PerfItKey))
            {
                OnRequestEnding(response, (PerfItContext) response.RequestMessage.Properties[Constants.PerfItKey]);
            }
        }

        public string CounterName { get; private set; }

        public CounterCreationData[] BuildCreationData(PerfItFilterAttribute filter)
        {
            return DoGetCreationData(filter);
        }
    }
}
