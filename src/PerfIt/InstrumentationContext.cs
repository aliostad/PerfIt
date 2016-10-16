using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace PerfIt
{
    public class InstrumentationContext : IDisposable
    {
        internal Stopwatch Stopwatch { get; private set; }

        private readonly InstrumentationContextDictionary _data;
 
        private readonly IList<PerfitHandlerContext> _contexts;

        public InstrumentationContextDictionary Data
        {
            get { return _data; }
        }

        internal IReadOnlyCollection<PerfitHandlerContext> ReadOnlyContexts
        {
            get { return new ReadOnlyCollection<PerfitHandlerContext>(_contexts); }
        }

        public InstrumentationContext(IDictionary<string, object> data,
            params PerfitHandlerContext[] contexts)
            : this(new InstrumentationContextDictionary(data), contexts)
        {
        }

        public InstrumentationContext(InstrumentationContextDictionary data,
            params PerfitHandlerContext[] contexts)
        {
            _data = data;
            _contexts = contexts.ToList();
            Stopwatch = Stopwatch.StartNew();
        }

        private void OnRequestEnding()
        {
            foreach (var counter in ReadOnlyContexts)
                counter.Handler.OnRequestEnding(Data);
        }

        /// <summary>
        /// Gets whether IsDisposed.
        /// </summary>
        protected bool IsDisposed
        {
            get { return _disposed; }
        }

        private bool _disposed;

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed || !disposing) return;
            OnRequestEnding();
        }

        public void Dispose()
        {
            Dispose(true);
            _disposed = true;
        }
    }
}
