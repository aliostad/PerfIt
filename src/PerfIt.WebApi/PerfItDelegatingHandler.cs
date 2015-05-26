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

    // NOTE: Due to nature of delegatinghandler, it is not possible to use aspect

    public class PerfItDelegatingHandler : DelegatingHandler
    {
        private Dictionary<string, PerfitHandlerContext> _counterContexts =
            new Dictionary<string, PerfitHandlerContext>();

        public bool PublishCounters { get; set; }

        public bool RaisePublishErrors { get; set; }

        public bool PublishEvent { get; set; }

        private string _categoryName;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryName">Name of the grouping category of counters (e.g. Process, Processor, Network Interface are all categories)
        /// if not provided, it will use name of the assembly.
        /// </param>
        public PerfItDelegatingHandler(string categoryName = null, 
            IInstrumentationDiscoverer discoverer = null,
            bool publishCounters = true,
            bool raisePublishErrors = true,
            bool publishEvent = true)
        {

            PublishCounters = publishCounters;
            RaisePublishErrors = raisePublishErrors;
            PublishEvent = publishEvent;

            SetPublish();
            SetErrorPolicy();
            SetEventPolicy();

            discoverer = discoverer ?? new FilterDiscoverer();
            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = assembly.GetName().Name;

            _categoryName = categoryName;

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
                        _counterContexts.Add(counterHandler.UniqueName, new PerfitHandlerContext()
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

        protected void SetEventPolicy()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishEvent] ?? PublishEvent.ToString();
            PublishEvent = Convert.ToBoolean(value);
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

            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = null;
            stopwatch.Stop();

            response = await base.SendAsync(request, cancellationToken);
            var ctx = (PerfItContext) response.RequestMessage.Properties[Constants.PerfItKey];
            if (PublishEvent && response.RequestMessage.Properties.ContainsKey(Constants.PerfItInstanceNameKey))
            {
                var instanceName = (string) response.RequestMessage.Properties[Constants.PerfItInstanceNameKey];
                
                InstrumentationEventSource.Instance.WriteInstrumentationEvent(
                    _categoryName, instanceName, stopwatch.ElapsedMilliseconds,
                    response.RequestMessage.Properties.ContainsKey(Constants.PerfItInstanceNameKey)
                       ? (string) response.RequestMessage.Properties[Constants.PerfItInstanceNameKey]
                        : response.RequestMessage.RequestUri.AbsoluteUri);
            }
            

            try
            {
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

}
