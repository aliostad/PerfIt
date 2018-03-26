using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.Tracers
{
    /// <summary>
    /// Meant mainly for demonstratio purposes. In production, you are meant to use other transports/mechanisms
    /// Fields:
    ///     1- Category
    ///     2- Instance
    ///     3- CorrelationId
    ///     4- TimeTakenMilli
    ///     5- Text1
    ///     6- Text2
    ///     7- Numberic
    ///     8- Decimal
    /// </summary>
    public class SeparatedFileTracer : SimpleTracerBase
    {
        private readonly char _columnSeparator;
        private readonly string _recordSeparator;
        private readonly StreamWriter _writer;

        public SeparatedFileTracer(string fileName, char columnSeparator = '\t', string recordSeparator = "\r\n")
        {
            _columnSeparator = columnSeparator;
            _recordSeparator = recordSeparator;
            _writer = new StreamWriter(fileName, true);
        }

        protected override void CommitTrace(TraceData data)
        {
            _writer.Write(data.ToString(_columnSeparator));
            _writer.Write(_recordSeparator);
        }

        public override void Dispose()
        {
            base.Dispose();
            _writer.Dispose();
        }
    }
}
