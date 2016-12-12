using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PerfIt.Handlers
{
    public abstract class CounterHandlerBase : ICounterHandler
    {

        protected string _instanceName;
        protected string _categoryName;
        protected string _uniqueName;

        public CounterHandlerBase(
            string categoryName,
            string instanceName)
        {
            _categoryName = categoryName;
            _instanceName = instanceName;
            Name = CounterType;

            _uniqueName = PerfItRuntime.GetUniqueName(instanceName, Name);
        }

        public virtual void Dispose()
        {

        }

        /// <summary>
        /// type of counter. just a string identifier
        /// </summary>
        public abstract string CounterType { get; }

        /// <summary>
        /// called when request arrives in delegating handler
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param> 
        protected abstract void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context);

        /// <summary>
        /// called as the async continuation on the delegating handler (when response is sent back)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="context"></param>
        protected abstract void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newInstanceName"></param>
        protected abstract void BuildCounters(bool newInstanceName = false);


        /// <summary>
        /// Handler to return data for creating counters
        /// </summary>
        /// <param name="filter">Filter attribute defined</param>
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
            if (exception.Message.Contains("already exists with a lifetime of Process"))
            {
                BuildCounters(true);
                Trace.TraceInformation("Now rebuilt with better look.");
                OnRequestStarting(contextBag, (PerfItContext)contextBag[Constants.PerfItKey]);
                return true;
            }

            return false;
        }

        public void OnRequestEnding(IDictionary<string, object> contextBag)
        {
            if (contextBag.ContainsKey(Constants.PerfItKey))
            {
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
        }

        public string Name { get; private set; }

        public CounterCreationData[] BuildCreationData()
        {
            return DoGetCreationData();
        }

        protected string GetInstanceName(bool newName = false)
        {
            const int SafeLength = 100;
            var len = _instanceName.Length;
            var instanceName = _instanceName;
            if (instanceName.Length > SafeLength)
            {
                instanceName = instanceName.Substring(len - SafeLength);
            }

            var name =
                instanceName +
                (newName ? "_" + Guid.NewGuid().ToString("N").Substring(12) : string.Empty);


            if(newName)
                Trace.TraceInformation("GetInstanceName - New name => " + name);

            return name;
        }

      

    }
}
