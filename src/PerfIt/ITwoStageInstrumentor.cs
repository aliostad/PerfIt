using System;

namespace PerfIt
{
    /// <summary>
    /// Provides a Two Stage <see cref="IInstrumentor"/>.
    /// </summary>
    public interface ITwoStageInstrumentor : IDisposable
    {
        /// <summary>
        /// Starts the <see cref="IInstrumentor"/> Start point.
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <returns></returns>
        /// <see cref="Constants.DefaultSamplingRate"/>
        object Start(double samplingRate = Constants.DefaultSamplingRate);
        
        /// <summary>
        /// Provides the <see cref="IInstrumentor"/> Finish point.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="instrumentationContext"></param>
        void Finish(object token, string instrumentationContext = null);
    }
}
