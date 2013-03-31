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
        void OnRequestStarting(HttpRequestMessage request);
        void OnRequestEnding(HttpResponseMessage response);
        string Name { get; }
        CounterCreationData[] BuildCreationData();
    }
}
