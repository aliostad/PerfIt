using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;
using Microsoft.Azure.EventHubs;
using Psyfon;

namespace PerfIt.Zipkin.EventHub
{
    public class EventHubDispatcher : IDispatcher
    {
        private readonly ISpanSerializer _spanSerializer = new ThriftSpanSerializer();
        private readonly IEventDispatcher _dispatcher;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="dispatcher">and EH dispatcher that must have been started.</param>
        public EventHubDispatcher(IEventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Dispose()
        {
            // none - DO NOT DISPOSE DISPATCHER YOU DID NOT CREATE!!
        }

        public void Emit(Span span)
        {
            var ms = new MemoryStream();
            _spanSerializer.SerializeTo(ms, span);
            _dispatcher.Add(new EventData(ms.ToArray()));
        }
    }
}
