using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    /// <summary>
    /// Disposable Counter Handler interface.
    /// </summary>
    public interface ICounterHandler : IDisposable
    {
        /// <summary>
        /// Gets the CounterType.
        /// </summary>
        string CounterType { get; }

        void OnRequestStarting(IDictionary<string, object> contextBag);

        void OnRequestEnding(IDictionary<string, object> contextBag);

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns a set of <see cref="CounterCreationData"/>.
        /// </summary>
        /// <returns></returns>
        IEnumerable<CounterCreationData> BuildCreationData();
    }
}
