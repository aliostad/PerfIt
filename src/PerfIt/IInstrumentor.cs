using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public interface IInstrumentor
    {
        void Instrument(Action aspect, string instrumentationContext = null);

        Task InstrumentAsync(Func<Task> asyncAspect, string instrumentationContext = null);
    }
}
