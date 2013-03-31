using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace PerfIt
{
    public class PerfItDelegatingHandler : DelegatingHandler
    {
        private HttpConfiguration _configuration;
        private Dictionary<string, PerfItCounterContext> _counterContexts = 
            new Dictionary<string, PerfItCounterContext>();

        private readonly string _applicationName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">Hosting configuration</param>
        /// <param name="applicationName">Name of the web application. It will be used as counetrs instance name</param>
        public PerfItDelegatingHandler(HttpConfiguration configuration, string applicationName)
        {
            _applicationName = applicationName;
            _configuration = configuration;
            var filters = PerfItRuntime.FindAllFilters();
            foreach (var filter in filters)
            {
                foreach (var counterType in filter.Counters)
                {
                    if(!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not registered: " + counterType);

                    var counterHandler = PerfItRuntime.HandlerFactories[counterType](applicationName, filter);
                    _counterContexts.Add(counterHandler.CounterName, new PerfItCounterContext()
                                                                         {
                                                                             Handler = counterHandler,
                                                                             Name = filter.Name
                                                                         });
                }
                    
            }

        }

        public string ApplicationName
        {
            get { return _applicationName; }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // check whether turned off in config
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishCounters];
            if (!string.IsNullOrEmpty(value))
            {
                if(!Convert.ToBoolean(value))
                    return base.SendAsync(request, cancellationToken);
            }

            request.Properties.Add(Constants.PerfItKey, new PerfItContext());
            foreach (var context in _counterContexts.Values)
            {
                context.Handler.OnRequestStarting(request);
            }

            return base.SendAsync(request, cancellationToken)
                .Then((response) =>
                                  {
                                      foreach (var context in _counterContexts.Values)
                                      {
                                          context.Handler.OnRequestEnding(response);
                                      }
                                      return response;
                                  });

        }

        private class PerfItCounterContext
        {
            public string Name { get; set; }
            public ICounterHandler Handler { get; set; }

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var context in _counterContexts.Values)
                {
                    context.Handler.Dispose();
                }
                _counterContexts.Clear();
            }
        }
    }
}
