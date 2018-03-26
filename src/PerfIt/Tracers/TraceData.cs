using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.Tracers
{
    public class TraceData
    {
        public TraceData(IInstrumentationInfo info, long timeTakenMilli, 
            string correlationId, InstrumentationContext context)
        {
            TimeTakenMilli = timeTakenMilli;
            Info = info;
            Context = context;
            CorrelationId = correlationId;
        }
        public long TimeTakenMilli { get; }
        public string CorrelationId { get; }
        public InstrumentationContext Context { get; }

        public IInstrumentationInfo Info { get; }

        public string ToString(char separator)
        {
            var sb = new StringBuilder();
            if(Info == null)
            {
                sb.Append(separator);
                sb.Append(separator);
            }
            else
            {
                sb.Append(Info.CategoryName);
                sb.Append(separator);
                sb.Append(Info.InstanceName);
                sb.Append(separator);
            }

            sb.Append(CorrelationId);
            sb.Append(separator);
            sb.Append(TimeTakenMilli);
            sb.Append(separator);

            if(Context == null)
            {
                sb.Append(separator);
                sb.Append(separator);
                sb.Append(separator);
                sb.Append(separator);
            }
            else
            {
                sb.Append(Context.Text1);
                sb.Append(separator);
                sb.Append(Context.Text2);
                sb.Append(separator);
                sb.Append(Context.Numeric);
                sb.Append(separator);
                sb.Append(Context.Decimal);
            }

            return sb.ToString();
        }
    }
}
