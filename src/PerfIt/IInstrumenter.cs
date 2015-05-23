using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public interface IInstrumenter
    {
        void Instrument(Action aspect);

        Task InstrumentAsync(Func<Task> asyncAspect);
    }
}
