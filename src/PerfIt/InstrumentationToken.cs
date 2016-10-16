namespace PerfIt
{
    /// <summary>
    /// Provides a Token for Instrumentation.
    /// </summary>
    public class InstrumentationToken
    {
        /// <summary>
        /// Gets or sets the Context.
        /// </summary>
        public InstrumentationContext Context { get; set; }

        /// <summary>
        /// Gets or sets the SamplingRate.
        /// </summary>
        public double SamplingRate { get; set; }
    }
}
