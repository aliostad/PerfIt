using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public class SimpleInstrumenter : IInstrumenter
    {

        private IInstrumentationInfo _info;
        private string _categoryName;
        

        public SimpleInstrumenter(IInstrumentationInfo info, string categoryName)
        {
            _categoryName = categoryName;
            _info = info;


        }

        public void Instrument(Action aspect)
        {
            
        }

        public Task InstrumentAsync(Func<Task> asyncAspect)
        {
            throw new NotImplementedException();
        }
    }
}
