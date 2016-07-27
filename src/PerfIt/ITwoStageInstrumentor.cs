using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public interface ITwoStageInstrumentor
    {
        object Start();
        
        void Finish(object token, string instrumentationContext = null);
    }
}
