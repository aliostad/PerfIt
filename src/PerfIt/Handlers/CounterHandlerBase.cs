using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt.Handlers
{
    public abstract class CounterHandlerBase : ICounterHandler
    {
        protected string CategoryName { get; private set; }

        protected string InstanceName { get; private set; }

        protected readonly string _uniqueName;

        protected CounterHandlerBase(string categoryName, string instanceName)
        {
            CategoryName = categoryName;
            InstanceName = instanceName;
            Name = CounterType;
            _uniqueName = PerfItRuntime.GetUniqueName(instanceName, Name);
        }

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
        protected abstract CounterCreationData[] DoGetCreationData();

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

        public CounterCreationData[] BuildCreationData()
        {
            return DoGetCreationData();
        }

        protected string GetInstanceName(bool newName = false)
        {
            var name = InstanceName + (newName ? "_" + Guid.NewGuid().ToString("N").Substring(6) : string.Empty);

            if (newName) Trace.TraceInformation("GetInstanceName - New name => " + name);

            return name;
        }
    }
}
