using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PerfIt.Handlers
{
    public class CurrentConcurrentCountHandler : CounterHandlerBase
    {
        private Lazy<PerformanceCounterCategory> _category;

        private Lazy<PerformanceCounter> _counter;

        public CurrentConcurrentCountHandler(string categoryName, string instanceName)
            : base(categoryName, instanceName)
        {
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
            var categoryName = _category.Value.CategoryName;
            _counter = new Lazy<PerformanceCounter>(() =>
            {
                var instanceName = GetInstanceName(newInstanceName);

                var counter = new PerformanceCounter
                {
                    CategoryName = categoryName,
                    CounterName = Name,
                    InstanceName = instanceName,
                    ReadOnly = false,
                    InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
                    RawValue = 0
                };

                return counter;
            });
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            return new[]
            {
                new CounterCreationData
                {
                    CounterName = Name,
                    CounterType = PerformanceCounterType.NumberOfItems32,
                    CounterHelp = "# of requests running concurrently"
                }
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
