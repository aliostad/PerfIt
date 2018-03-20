using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    /// <summary>
    /// A An abstraction for the typical instrumentor
    /// </summary>
    public interface ITwoStageInstrumentor
    {
        object Start(double samplingRate = Constants.DefaultSamplingRate);
        
        void Finish(object token, string instrumentationContext = null, ExtraContext extraContext = null);
    }
}
