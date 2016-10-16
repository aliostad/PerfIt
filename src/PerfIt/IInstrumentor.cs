using System;
using System.Threading.Tasks;

namespace PerfIt
{
    /// <summary>
    /// Instrumentor interface.
    /// </summary>
    public interface IInstrumentor : IDisposable
    {
        /// <summary>
        /// Provides an Instrument entry point.
        /// </summary>
        /// <param name="aspect"></param>
        /// <param name="instrumentationContext"></param>
        /// <param name="samplingRate"></param>
        /// <see cref="Constants.DefaultSamplingRate"/>
        void Instrument(Action aspect, string instrumentationContext = null,
            double samplingRate = Constants.DefaultSamplingRate);

        /// <summary>
        /// Provides an Asynchronous Instrument entry point.
        /// </summary>
        /// <param name="asyncAspect"></param>
        /// <param name="instrumentationContext"></param>
        /// <param name="samplingRate"></param>
        /// <returns></returns>
        /// <see cref="Constants.DefaultSamplingRate"/>
        Task InstrumentAsync(Func<Task> asyncAspect, string instrumentationContext = null,
            double samplingRate = Constants.DefaultSamplingRate);
    }
}
