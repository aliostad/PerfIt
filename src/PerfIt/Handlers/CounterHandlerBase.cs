using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    /// <summary>
    /// Counter Handler base clas.
    /// </summary>
    public abstract class CounterHandlerBase : ICounterHandler
    {
        /// <summary>
        /// Gets the CategoryName.
        /// </summary>
        protected string CategoryName { get; private set; }

        /// <summary>
        /// Gets the InstanceName.
        /// </summary>
        protected string InstanceName { get; private set; }

        /// <summary>
        /// Gets the UniqueName.
        /// </summary>
        protected string UniqueName { get; private set; }

        /// <summary>
        /// Protected Constructor
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
        protected CounterHandlerBase(string categoryName, string instanceName)
        {
            CategoryName = categoryName;
            InstanceName = instanceName;
            Name = CounterType;
            UniqueName = PerfItRuntime.GetUniqueName(instanceName, Name);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Gets the Type of Counter.
        /// </summary>
        /// <remarks>Just a string identifier.</remarks>
        public abstract string CounterType { get; }

        /// <summary>
        /// called when request arrives in delegating handler
        /// </summary>
        /// <param name="contextBag"></param>
        /// <param name="context"></param> 
        protected abstract void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context);

        /// <summary>
        /// called as the async continuation on the delegating handler (when response is sent back)
        /// </summary>
        /// <param name="contextBag"></param>
        /// <param name="context"></param>
        protected abstract void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newInstanceName"></param>
        protected abstract void BuildCounters(bool newInstanceName = false);

        /// <summary>
        /// Handler to return data for creating counters.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<CounterCreationData> DoGetCreationData();

        public void OnRequestStarting(IDictionary<string, object> contextBag)
        {
            if (contextBag.ContainsKey(Constants.PerfItKey))
            {
                try
                {
                    OnRequestStarting(contextBag, (PerfItContext) contextBag[Constants.PerfItKey]);
                }
                catch (Exception exception) // changed to do on exception name
                {
                    Trace.TraceError(exception.ToString());
                    if (!CaterForWorkerProcessRecycle(exception, contextBag))
                        throw;
                }
            }
        }

        private bool CaterForWorkerProcessRecycle(Exception exception, IDictionary<string, object> contextBag)
        {
            if (!exception.Message.Contains("already exists with a lifetime of Process"))
                return false;
            BuildCounters(true);
            Trace.TraceInformation("Now rebuilt with better look.");
            OnRequestStarting(contextBag, (PerfItContext)contextBag[Constants.PerfItKey]);
            return true;
        }

        public void OnRequestEnding(IDictionary<string, object> contextBag)
        {
            if (contextBag.ContainsKey(Constants.PerfItKey)) return;
            try
            {
                OnRequestEnding(contextBag, (PerfItContext) contextBag[Constants.PerfItKey]);
            }
            catch (Exception exception) // changed to do on exception name 
            {
                Trace.TraceError(exception.ToString());
                if (!CaterForWorkerProcessRecycle(exception, contextBag))
                    throw;
            }
        }

        public string Name { get; private set; }

        public IEnumerable<CounterCreationData> BuildCreationData()
        {
            return DoGetCreationData();
        }

        /// <summary>
        /// Returns the InstanceName given <paramref name="newName"/>.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        protected string GetInstanceName(bool newName = false)
        {
            var name = InstanceName + (newName ? "_" + Guid.NewGuid().ToString("N").Substring(6) : string.Empty);

            if (newName) Trace.TraceInformation("GetInstanceName - New name => " + name);

            return name;
        }
    }
}
