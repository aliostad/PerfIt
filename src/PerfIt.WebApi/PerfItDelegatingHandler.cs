using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PerfIt.WebApi;

namespace PerfIt
{
    public class PerfItDelegatingHandler : DelegatingHandler
    {
        private Dictionary<string, PerfItCounterContext> _counterContexts =
            new Dictionary<string, PerfItCounterContext>();

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryName">Name of the grouping category of counters (e.g. Process, Processor, Network Interface are all categories)
        /// if not provided, it will use name of the assembly.
        /// </param>
        public PerfItDelegatingHandler(string categoryName = null, IInspectDiscoverer discoverer = null)
        {

            PublishCounters = true;
            RaisePublishErrors = true;

            SetPublish();
            SetErrorPolicy();

            discoverer = discoverer ?? new FilterDiscoverer();
            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = assembly.GetName().Name;

            var filters = discoverer.Discover(assembly);
            foreach (var filter in filters)
            {
                foreach (var counterType in filter.Counters)
                {
                    if (!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not registered: " + counterType);

                    var counterHandler = PerfItRuntime.HandlerFactories[counterType](categoryName, filter.InstanceName);
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

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {

            if (!PublishCounters)
                return await base.SendAsync(request, cancellationToken);
            try
            {
                // check whether turned off in config

                request.Properties.Add(Constants.PerfItKey, new PerfItContext());
                foreach (var context in _counterContexts.Values)
                {
                    context.Handler.OnRequestStarting(request.Properties);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());

                if (RaisePublishErrors)
                    throw;
            }


            var response = await base.SendAsync(request, cancellationToken);

            try
            {
                var ctx = (PerfItContext)response.RequestMessage.Properties[Constants.PerfItKey];

                foreach (var counter in ctx.CountersToRun)
                {
                    _counterContexts[counter].Handler.OnRequestEnding(response.RequestMessage.Properties);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (RaisePublishErrors)
                    throw;
            }

            return response;
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

    internal class PerfItCounterContext
    {
        public string Name { get; set; }
        public ICounterHandler Handler { get; set; }

    }
}
