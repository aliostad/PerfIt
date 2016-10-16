using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Xunit;

namespace PerfIt.Castle.Interception.Tests
{
    public class AttributeTest
    {
        private const int DelayInMs = 100;

        private readonly WindsorContainer _container;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        /// <summary>
        /// Listener backing field.
        /// </summary>
        /// <remarks>May not want to convert this to a local, especially if the reference is desirable.</remarks>
        private readonly ObservableEventListener _listener;

        private readonly SinkSubscription<InMemoryEventSink> _subscription;

        public AttributeTest()
        {
            _container = new WindsorContainer();

            _container.Register(Component.For<SimpleClass>()
                .ImplementedBy<SimpleClass>().Interceptors<PerfItInterceptor>());

            _container.Register(Component.For<PerfItInterceptor>()
                .UsingFactoryMethod((k, ctx) =>
                {
                    var interceptor = new PerfItInterceptor("Vashah")
                    {
                        PublishCounters = false,
                        PublishEvent = true,
                        RaisePublishErrors = true,
                        SamplingRate = 1.0d
                    };
                    //interceptor.InstrumentorRequired +=
                    //    (sender, e) => e.Instrumentor = new SimpleInstrumentorFixture(e.Info);
                    return interceptor;
                })
                );

            _listener = new ObservableEventListener();
            _listener.EnableEvents(InstrumentationEventSource.Instance, EventLevel.Verbose);
            _subscription = _listener.LogToMemory();
        }

        /// <summary>
        /// 1d
        /// </summary>
        private const double Epsilon = 1d;

        [Fact]
        public void Delay_GetsCapturedCorrectly()
        {
            var simpleClass = _container.Resolve<SimpleClass>();
            Assert.NotNull(simpleClass);
            simpleClass.Delay(DelayInMs);
            Thread.Sleep(100);
            EventEntry entry;
            Assert.True(_subscription.Sink.Entries.TryDequeue(out entry), "Nothing in the queue");
            var actual = (double) entry.Payload[2];
            Console.WriteLine(actual);
            actual.AssertGreaterThanOrEqual(DelayInMs,
                string.Format("Captured delay was shorter: was {0} expected {1}", actual, DelayInMs), Epsilon);
        }

        [Fact]
        public async Task DelayAsync_GetsCapturedCorrectly()
        {
            var simpleClass = _container.Resolve<SimpleClass>();
            Assert.NotNull(simpleClass);
            simpleClass.DelayAsync(DelayInMs).Wait();
            Thread.Sleep(100);
            EventEntry entry;
            Assert.True(_subscription.Sink.Entries.TryDequeue(out entry), "Nothing in the queue");

            /* TODO: TBD: this one is failing on account of the Delayed Task returning too soon;
             * after some SO research, this is more of a Thread/Task issue than it is PerfIt Attribute one...
             * tried several of the workarounds, none of which seem to work */

            var actual = (double) entry.Payload[2];
            Console.WriteLine(actual);
            actual.AssertGreaterThanOrEqual(DelayInMs,
                string.Format("Captured delay was shorter: was {0} expected {1}", actual, DelayInMs), Epsilon);
        }
    }
}
