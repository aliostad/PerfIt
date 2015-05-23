using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PerfIt
{
    public interface ICounterHandler : IDisposable
    {
        string CounterType { get; }

        void OnRequestStarting(IDictionary<string, object> contextBag);

        void OnRequestEnding(IDictionary<string, object> contextBag);

        string Name { get; }

        string UniqueName { get; }
        CounterCreationData[] BuildCreationData();
    }
}
