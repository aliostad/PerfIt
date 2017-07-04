using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public interface ITwoStageTracer
    {
        object Start(IInstrumentationInfo info);

        void Finish(object token, string instrumentationContext = null);
    }
}
