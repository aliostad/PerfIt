using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt
{
    /// <summary>
    /// Extra information for the context
    /// </summary>
    public class InstrumentationContext
    {
        public string Text1 { get; set; }

        public string Text2 { get; set; }

        public int Numeric { get; set; }

        public double Decimal { get; set; }
    }
}
