using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;
using Microsoft.ServiceBus.Messaging;

namespace PerfIt.Zipkin.EventHub
{
    public class EventHubDispatcher : IDispatcher
    {
        private readonly EventHubClient _eventHubClient;
        private readonly ISpanSerializer _spanSerializer = new ThriftSpanSerializer();
        private ConcurrentQueue<Message>
            _internalQueue = new ConcurrentQueue<Message>();

        private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(1);
        private int _minBatchSize;
        private int _maxBatchSize;
        private int _messageExpirySeconds;
        private int _retryCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">EVentHub connection string</param>
        /// <param name="eventHubName">Name of the eventhub</param>
        /// <param name="minBatchSize">minimum batch size to dispatch messages to eventhub</param>
        /// <param name="maxBatchSize">maximum batch size to dispatch messages to eventhub</param>
        /// <param name="retryCount">Number of times a failed message ina batch will be retried for dispatch</param>
        /// <param name="messageExpirySeconds">Messages that have exhasted their retry and past their expiry will be discarded</param>
        public EventHubDispatcher(string connectionString, string eventHubName, 
            int minBatchSize = 5,
            int maxBatchSize = 100,
            int retryCount = 2,
            int messageExpirySeconds = 2*60            )
        {
            _retryCount = retryCount;
            _messageExpirySeconds = messageExpirySeconds;
            _maxBatchSize = maxBatchSize;
            _minBatchSize = minBatchSize;
            _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
        }

        public Task EmitBatchAsync(IEnumerable<Span> spans)
        {
            var s = new MemoryStream();
            foreach (var span in spans)
            {
                _spanSerializer.SerializeTo(s, span);
            }

            _internalQueue.Enqueue(new Message(new EventData(s.ToArray())));
            return DoEmitAsync(_minBatchSize);
        }

        private Task DoEmitAsync(int minBatchSize)
        {
            if(_internalQueue.Count < minBatchSize)
                return Task.FromResult(false);

            var list = new List<Message>();
            Message item = null;
            while (list.Count < _maxBatchSize && _internalQueue.TryDequeue(out item))
            {
                if (item.RetryCount < _retryCount && !item.IsExpired(_messageExpirySeconds))                
                list.Add(item);
            }

            try
            {
                Console.WriteLine($"Attempting to dispatch {list.Count} messages - EventHubDispatcher");
                return _eventHubClient.SendBatchAsync(list.Select(x => x.Data));
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                list.ForEach(x => _internalQueue.Enqueue(x.IncrementError()));
                return Task.FromResult(false);
            }

        }

        public void Dispose()
        {
            try
            {
                DoEmitAsync(0).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
                // none
            }
        }

        private class Message
        {
            public Message(EventData data)
            {
                Data = data;
                Time = DateTimeOffset.Now;
            }

            public EventData Data { get; }

            public DateTimeOffset Time { get; }

            public int RetryCount { get; private set; }

            public Message IncrementError()
            {
                RetryCount += 1;
                return this;
            }

            public bool IsExpired(int expirySeconds)
            {
                return DateTimeOffset.Now.Subtract(Time).TotalSeconds >= expirySeconds;
            }
        }
    }
}
