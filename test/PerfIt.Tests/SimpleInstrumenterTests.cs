using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Xunit;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;

namespace PerfIt.Tests
{
    public class SimpleInstrumenterTests
    {
        private const string TestCategory = "PerfItTests";

        public SimpleInstrumenterTests()
        {
            PerfItRuntime.InstallStandardCounters(TestCategory);

        }

        [Fact]
        public void CanPublishAspect()
        {

            var ins = new SimpleInstrumenter(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance"
            }, TestCategory);

            var formatter =
                new JsonEventTextFormatter(EventTextFormatting.Indented);

            var listener = ConsoleLog.CreateListener();
            listener.EnableEvents(InstrumentationEventSource.Instance, EventLevel.LogAlways,
                Keywords.All);
            
            ins.Instrument(() => { Thread.Sleep(100); }, "test...");
     
            listener.DisableEvents(InstrumentationEventSource.Instance);
            listener.Dispose();
        }

        [Fact]
        public void CanPublishAsyncAspect()
        {
            var ins = new SimpleInstrumenter(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance"
            }, TestCategory);

            ins.InstrumentAsync(async () => { await Task.Delay(100); }, "test...").Wait();

        }
    }
}
