using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing;
using Criteo.Profiling.Tracing.Tracers.Zipkin;
using Moq;
using Xunit;

namespace PerfIt.Zipkin.Tests
{
    public class SpanEmitterTests
    {
        [Fact]
        public void UsingItWillNotPreventAppShuttingDown()
        {
            SpanEmitHub.Instance.RegisterDispatcher(new ConsoleDispatcher());
        }

        [Fact]
        public void EmittingDoesNotThrow()
        {
            SpanEmitHub.Instance.ClearDispatchers();
            SpanEmitHub.Instance.Emit(new Span(Trace.Create().CurrentSpan, DateTime.Now));
        }

        [Fact]
        public void EmittingGetsFinallyEmitted()
        {
            var mock = new Mock<IDispatcher>();
            var span = new Span(Trace.Create().CurrentSpan, DateTime.Now);
            mock.Setup(x => x.EmitBatchAsync(It.Is<IEnumerable<Span>>(y => y.First() == span)))
                .Returns(Task.FromResult(false));
            
            SpanEmitHub.Instance.RegisterDispatcher(mock.Object);
            SpanEmitHub.Instance.Emit(span);

            Thread.Sleep(500);           
            mock.VerifyAll();
        }

        // manual check for output
        [Fact]
        public void EmittingToTraceDoesNotThrow()
        {
            var span = new Span(Trace.Create().CurrentSpan, DateTime.Now);
            SpanEmitHub.Instance.RegisterDispatcher(new TraceDispatcher());
            SpanEmitHub.Instance.Emit(span);

            Thread.Sleep(1000);

        }

        // manual check for output
        [Fact]
        public void EmittingToConsoleDoesNotThrow()
        {
            var span = new Span(Trace.Create().CurrentSpan, DateTime.Now);
            SpanEmitHub.Instance.RegisterDispatcher(new ConsoleDispatcher());
            SpanEmitHub.Instance.Emit(span);

            Thread.Sleep(1000);
        }
    }
}
