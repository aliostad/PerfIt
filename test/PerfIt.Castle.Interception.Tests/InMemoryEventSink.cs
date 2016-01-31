using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace PerfIt.Castle.Interception.Tests
{
    public class InMemoryEventSink : IObserver<EventEntry>
    {
        private ConcurrentQueue<EventEntry> _entries = new ConcurrentQueue<EventEntry>();

        public ConcurrentQueue<EventEntry> Entries
        {
            get { return _entries; }
        }

        public void OnNext(EventEntry value)
        {
            if (value != null)
            {
                _entries.Enqueue(value);
            }
        }

        public void OnError(Exception error)
        {
            
        }

        public void OnCompleted()
        {
            
        }
    }

    public static class InMemoryEventSinkExtensions
    {
        public static SinkSubscription<InMemoryEventSink> LogToMemory(this IObservable<EventEntry> eventStream)
        {
            var sink = new InMemoryEventSink();
            var subscription = eventStream.Subscribe(sink);
            return new SinkSubscription<InMemoryEventSink>(subscription, sink);
        }
    }
}
