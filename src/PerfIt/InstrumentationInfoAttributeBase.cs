using System;

namespace PerfIt
{
    /// <summary>
    /// Provides a common <see cref="IInstrumentationInfo"/> decration foundation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class InstrumentationInfoAttributeBase : Attribute, IInstrumentationInfo
    {
        /// <summary>
        /// Gets the Info.
        /// </summary>
        public IInstrumentationInfo Info { get; private set; }

        /// <summary>
        /// ProtectedConstructor.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="description"></param>
        protected InstrumentationInfoAttributeBase(string categoryName, string description = null)
        {
            /* Decorator should wrap the Info instead of being an Info. That gets us
             * away from Reflection overhead as early as possible. */

            Info = new InstrumentationInfo
            {
                CategoryName = categoryName,
                Description = description ?? string.Empty
            };
        }

        /// <summary>
        /// Optional name of the Counter.  If not specified it will be [Controller].[Action]
        /// for each Counter. If it is provided, make sure it is UNIQUE within the project.
        /// </summary>
        public string InstanceName
        {
            get { return Info.InstanceName; }
            set { Info.InstanceName = value; }
        }

        /// <summary>
        /// Description of the Counter. Will be published to Counter metadata visible in Perfmon.
        /// </summary>
        public string Description
        {
            get { return Info.Description; }
            set { Info.Description = value; }
        }

        /// <summary>
        /// Counter types. Each value as a <see cref="System.String"/>.
        /// </summary>
        public string[] Counters
        {
            get { return Info.Counters; }
            set { Info.Counters = value; }
        }

        public string CategoryName
        {
            get { return Info.CategoryName; }
            set { Info.CategoryName = value; }
        }

        public bool PublishCounters
        {
            get { return Info.PublishCounters; }
            set { Info.PublishCounters = value; }
        }

        public bool RaisePublishErrors
        {
            get { return Info.RaisePublishErrors; }
            set { Info.RaisePublishErrors = value; }
        }

        public bool PublishEvent
        {
            get { return Info.PublishEvent; }
            set { Info.PublishEvent = value; }
        }

        public bool RequiresInstrumentationContext
        {
            get { return Info.RequiresInstrumentationContext; }
        }

        public double SamplingRate
        {
            get { return Info.SamplingRate; }
            set { Info.SamplingRate = value; }
        }
    }
}
