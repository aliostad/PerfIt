using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Xunit;

namespace PerfIt.Castle.Interception.Tests
{
    public class AttributeTest
    {
        private const int DelayInMs = 100;

        private WindsorContainer _container;
        private ObservableEventListener _listener;
        private SinkSubscription<InMemoryEventSink> _subscription;

        public AttributeTest()
        {
            _container = new WindsorContainer();
            _container.Register(
                Component.For<SimpleClass>().ImplementedBy<SimpleClass>()
                    .Interceptors<PerfItInterceptor>());
            _container.Register(Component.For<PerfItInterceptor>().Instance(
                new PerfItInterceptor("Vashah")
                {
                    PublishCounters = false,
                    PublishEvent = true,
                    RaisePublishErrors = true
                }));

            _listener = new ObservableEventListener();
            _listener.EnableEvents(InstrumentationEventSource.Instance, EventLevel.Verbose);
            _subscription = _listener.LogToMemory();
        }

        [Fact]
        public void Delay_GetsCapturedCorrectly()
        {
            var simpleClass = _container.Resolve<SimpleClass>();
            simpleClass.Delay(DelayInMs);
            Thread.Sleep(100);
            EventEntry entry = null;
            Assert.True(_subscription.Sink.Entries.TryDequeue(out entry), "Nothing in the queue");
            Console.WriteLine(entry.Payload[2]);
            Assert.True((long)entry.Payload[2] > DelayInMs, "Captured delay was shorter");
        }

        [Fact]
        public async Task DelayAsync_GetsCapturedCorrectly()
        {
            var simpleClass = _container.Resolve<SimpleClass>();
            await simpleClass.DelayAsync(DelayInMs);
            Thread.Sleep(100);
            EventEntry entry = null;
            Assert.True(_subscription.Sink.Entries.TryDequeue(out entry), "Nothing in the queue");
            Console.WriteLine(entry.Payload[2]);
            Assert.True((long) entry.Payload[2] > DelayInMs, "Captured delay was shorter");
        }
    }
}
