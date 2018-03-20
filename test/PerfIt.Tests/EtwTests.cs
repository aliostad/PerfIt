using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PerfIt.Tests
{
    public class EtwTests
    {
        [Fact]
        public void CanCreateEventEvenIfPassedAllNulls()
        {
            InstrumentationEventSource.Instance.WriteInstrumentationEvent(null, null, 1, null, null, null);
        }
    }
}
