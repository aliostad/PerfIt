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
                catch (InvalidOperationException exception)
                {

                    Trace.TraceError(exception.ToString());
                    BuildCounters(true);
                    OnRequestStarting(contextBag, (PerfItContext) contextBag[Constants.PerfItKey]);                    
                }
            }
        }

        public void OnRequestEnding(IDictionary<string, object> contextBag)
        {
            if (contextBag.ContainsKey(Constants.PerfItKey))
            {
                try
                {
                    OnRequestEnding(contextBag, (PerfItContext) contextBag[Constants.PerfItKey]);

                }
                catch (InvalidOperationException exception)
                {
                    
                    Trace.TraceError(exception.ToString());
                    BuildCounters(true);
                    OnRequestEnding(contextBag, (PerfItContext) contextBag[Constants.PerfItKey]);
                }
            }
        }

        public string Name { get; private set; }
        public string UniqueName { get { return _uniqueName; } }

        public string GetUniqueName()
        {
            return _categoryName + _instanceName + Name;
        }

        public CounterCreationData[] BuildCreationData()
        {
            return DoGetCreationData();
        }

        protected string GetInstanceName(bool newName = false)
        {
            return
                _instanceName +
                (newName ? "_" + Guid.NewGuid().ToString("N").Substring(6) : string.Empty);
        }

      

    }
}
