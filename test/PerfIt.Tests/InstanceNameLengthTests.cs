using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PerfIt.Handlers;
using Xunit;

namespace PerfIt.Tests
{
    public class InstanceNameLengthTests
    {
        [Fact]
        public void PerfitWillNeverEverCreatingAnInstanceNameBiggerThan116Chars()
        {
            var largeString = string.Join("-", Enumerable.Range(0, 128).Select(i => i.ToString()));
            var handler = new DummyHandle("dummy", largeString);

            string withFalse = handler.GetInstanceNameCheat(false);
            string withTrue = handler.GetInstanceNameCheat(true);
            Assert.True(withFalse.Length < 127, withFalse.Length.ToString());
            Assert.True(withTrue.Length < 127, withTrue.Length.ToString());
        }

        [Fact]
        public void PerfitWorksWithSmallInstanceName()
        {
            var handler = new DummyHandle("dummy", "SmallString");

            string withFalse = handler.GetInstanceNameCheat(false);
            string withTrue = handler.GetInstanceNameCheat(true);
            Assert.True(withFalse.Length < 127, withFalse.Length.ToString());
            Assert.True(withTrue.Length < 127, withTrue.Length.ToString());
        }
        public class DummyHandle : CounterHandlerBase
        {
            public string GetInstanceNameCheat(bool newName)
            {
                return this.GetInstanceName(newName);
            }

            public DummyHandle(string categoryName, string instanceName) : base(categoryName, instanceName)
            {
            }

            public override string CounterType { get; }
            protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
            {
                throw new NotImplementedException();
            }

            protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
            {
                throw new NotImplementedException();
            }

            protected override void BuildCounters(bool newInstanceName = false)
            {
                throw new NotImplementedException();
            }

            protected override CounterCreationData[] DoGetCreationData()
            {
                throw new NotImplementedException();
            }
        }
    }
}
