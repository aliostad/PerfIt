using System.Collections.Generic;
using System.Diagnostics;
using PerfIt.Handlers;

namespace PerfIt.Tests.Stubs
{
    public class CustomCounterStub : CounterHandlerBase
    {
        public static int RequestStartCount { get; private set; }
        public static int RequestEndCount { get; private set; }

        public static void ClearCounters()
        {
            RequestStartCount = 0;
            RequestEndCount = 0;
        }

        public CustomCounterStub(string categoryName, string instanceName) : base(categoryName, instanceName)
        {            
        }

        public override string CounterType { get; }
        protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
        {
            RequestStartCount++;
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
            RequestEndCount++;
        }

        protected override void BuildCounters(bool newInstanceName = false)
        {            
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            return new CounterCreationData[] {};
        }
    }
}
