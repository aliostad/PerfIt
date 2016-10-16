using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PerfIt
{
    /// <summary>
    /// Current Concurrent Count Counter handler.
    /// </summary>
    public class CurrentConcurrentCountHandler : CounterHandlerBase
    {
        private Lazy<PerformanceCounterCategory> _category;

        private Lazy<PerformanceCounter> _counter;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
        public CurrentConcurrentCountHandler(string categoryName, string instanceName)
            : base(categoryName, instanceName)
        {
            // TODO: TBD: consider better placement for this call; possibly as part of a context, test fixture, etc?
            BuildCategories();
            BuildCounters();
        }

        public override string CounterType
        {
            get { return CounterTypes.CurrentConcurrentOperationsCount; }
        }

        protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
        {
            _counter.Value.Increment();
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
            _counter.Value.Decrement();
        }

        private void BuildCategories()
        {
            _category = new Lazy<PerformanceCounterCategory>(() =>
            {
                if (PerformanceCounterCategory.Exists(CategoryName))
                {
                    var category = PerformanceCounterCategory.GetCategories()
                        .SingleOrDefault(cat => cat.CategoryName == CategoryName);
                    return category;
                }

                var data = new CounterCreationDataCollection();
                const PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.Unknown;
                return PerformanceCounterCategory.Create(CategoryName, null, categoryType, data);
            });
        }

        protected override void BuildCounters(bool newInstanceName = false)
        {
            _counter = new Lazy<PerformanceCounter>(() => new PerformanceCounter
            {
                CategoryName = _category.Value.CategoryName,
                CounterName = Name,
                InstanceName = GetInstanceName(newInstanceName),
                ReadOnly = false,
                InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
                RawValue = 0
            });
        }

        protected override IEnumerable<CounterCreationData> DoGetCreationData()
        {
            yield return new CounterCreationData
            {
                CounterName = Name,
                CounterType = PerformanceCounterType.NumberOfItems32,
                CounterHelp = "# of requests running concurrently"
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            
            if (_counter != null && _counter.IsValueCreated)
            {
                _counter.Value.RemoveInstance();
                _counter.Value.Dispose();
            }

            if (_category != null && _category.IsValueCreated)
            {
                PerformanceCounterCategory.Delete(_category.Value.CategoryName);
            }
        }
    }
}
