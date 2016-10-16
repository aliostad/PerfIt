namespace PerfIt
{
    // TODO: TBD: could potentially call IInstrumentInfo ICloneable, but will leave that alone for now
    /// <summary>
    /// Provides InstrumentationInfo to the project.
    /// </summary>
    public interface IInstrumentationInfo
    {
        /// <summary>
        /// Optional name of the counter. 
        /// If not specified it will be [controller].[action] for each counter.
        /// If it is provided, make sure it is UNIQUE within the project
        /// </summary>
        string InstanceName { get; set; }

        /// <summary>
        /// Description of the counter. Will be published to counter metadata visible in Perfmon.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Counter types. Each value as a string.
        /// </summary>
        string[] Counters { get; set; }

        /// <summary>
        /// The categoryName of the stuff
        /// </summary>
        string CategoryName { get; set; }

        /// <summary>
        /// Whether publish windows performance counters
        /// </summary>
        bool PublishCounters { get; set; }

        /// <summary>
        /// Whether throw exceptions if publishing counters/events failed or only write to trace
        /// </summary>
        bool RaisePublishErrors { get; set; }

        /// <summary>
        /// Whether to publish ETW events
        /// </summary>
        bool PublishEvent { get; set; }

        /// <summary>
        /// Gets whether RequiresInstrumentationContext.
        /// </summary>
        bool RequiresInstrumentationContext { get; }

        /// <summary>
        /// A value between 0.0 and 1.0 as the proportion of calls to be sampled.
        /// Useful if you are generating a lot of ETW events
        /// </summary>
        double SamplingRate { get; set; }
    }
}
