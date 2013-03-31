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
            Name = filter.Name + "." + CounterType;
            _applicationName = applicationName;
        }

        public virtual void Dispose()
        {
            
        }

        /// <summary>
        /// type of counter. just a string identifier
        /// </summary>
        public abstract string CounterType { get; }

        /// <summary>
        /// called when request arrives in delegating handler
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param> 
        protected abstract void OnRequestStarting(HttpRequestMessage request, PerfItContext context);
        
        /// <summary>
        /// called as the async continuation on the delegating handler (when response is sent back)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="context"></param>
        protected abstract void OnRequestEnding(HttpResponseMessage response, PerfItContext context);
        
        /// <summary>
        /// Handler to return data for creating counters
        /// </summary>
        /// <param name="filter">Filter attribute defined</param>
        /// <returns></returns>
        protected abstract CounterCreationData[] DoGetCreationData();

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

        public string Name { get; private set; }

        public CounterCreationData[] BuildCreationData()
        {
            return DoGetCreationData();
        }

    }
}
