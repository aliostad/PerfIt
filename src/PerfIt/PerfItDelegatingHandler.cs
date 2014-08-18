using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
        private Dictionary<string, PerfItCounterContext> _counterContexts = 
            new Dictionary<string, PerfItCounterContext>();

        private readonly string _applicationName;
        private string _categoryName;
    

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">Hosting configuration</param>
        /// <param name="applicationName">Name of the web application. It will be used as counetrs instance name</param>
        public PerfItDelegatingHandler(HttpConfiguration configuration, // not used at the mo
            string categoryName)
        {
            _categoryName = categoryName;


            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;

            var filters = PerfItRuntime.FindAllFilters(assembly);
            foreach (var filter in filters)
            {
                foreach (var counterType in filter.Counters)
                {
                    if(!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not registered: " + counterType);

                    var counterHandler = PerfItRuntime.HandlerFactories[counterType](categoryName, filter.InstanceName, filter);
                    if (!_counterContexts.Keys.Contains(counterHandler.UniqueName))
                    {
                        _counterContexts.Add(counterHandler.UniqueName, new PerfItCounterContext()
                                                                             {
                                                                                 Handler = counterHandler,
                                                                                 Name = counterHandler.UniqueName
                                                                             });
                    }
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
                            var ctx = (PerfItContext) response.RequestMessage.Properties[Constants.PerfItKey];

                            foreach (var counter in ctx.CountersToRun)
                            {
                                _counterContexts[counter].Handler.OnRequestEnding(response);
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
