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
        private ConcurrentDictionary<string, PerfItCounterContext> _counterContexts = 
            new ConcurrentDictionary<string, PerfItCounterContext>();

        private readonly string _applicationName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">Hosting configuration</param>
        /// <param name="applicationName">Name of the application. It will be used as counetrs category</param>
        public PerfItDelegatingHandler(HttpConfiguration configuration, string applicationName)
        {
            _applicationName = applicationName;
            _configuration = configuration;
            var filters = PerfItRuntime.FindAllFilters();
            foreach (var filter in filters)
            {

                    
            }

        }

        /// <summary>
        /// Uses current assemnly name as the name of the application
        /// </summary>
        /// <param name="configuration">Hosting configuration</param>
        public PerfItDelegatingHandler(HttpConfiguration configuration):
            this(configuration, PerfItRuntime.GetDefaultApplicationName())
        {

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


            return null;

        }

        private class PerfItCounterContext
        {
            public string Name { get; set; }
            public ICounterHandler Handler { get; set; }

        }

    }
}
