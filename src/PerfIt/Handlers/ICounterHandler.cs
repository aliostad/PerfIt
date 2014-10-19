using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt
{
    public interface ICounterHandler : IDisposable
    {
        string CounterType { get; }
        void OnRequestStarting(IPerfItContext context);
        void OnRequestEnding(IPerfItContext context);

        string Name { get; }

        string UniqueName { get; }
        CounterCreationData[] BuildCreationData();
    }
}
