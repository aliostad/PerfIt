using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;
using Microsoft.ServiceBus.Messaging;

namespace PerfIt.Zipkin.EventHub
{
    public class EventHubEmitter : IEmitter
    {
        private readonly EventHubClient _eventHubClient;
        private readonly ISpanSerializer _spanSerializer = new ThriftSpanSerializer();

        public EventHubEmitter(string connectionString, string eventHubName)
        {
            _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
        }

        public Task EmitBatchAsync(IEnumerable<Span> spans)
        {
            var s = new MemoryStream();
            foreach (var span in spans)
            {
                _spanSerializer.SerializeTo(s, span);
            }
            
            return _eventHubClient.SendAsync(
                new EventData(s.ToArray()));
        }
    }
}
