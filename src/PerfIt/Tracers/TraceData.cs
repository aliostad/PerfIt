using Newtonsoft.Json;
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
            TimestampUtc = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// UTC Timestamp of the trace 
        /// </summary>
        public DateTimeOffset TimestampUtc { get; }

        /// <summary>
        /// How long in millis it took
        /// </summary>
        [JsonProperty("time")]
        public long TimeTakenMilli { get; }

        /// <summary>
        /// CorrelationId
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// Optional extra context
        /// </summary>
        public InstrumentationContext Context { get; }

        /// <summary>
        /// 
        /// </summary>
        public IInstrumentationInfo Info { get; }

        /// <summary>
        /// Creates a string representation
        /// </summary>
        /// <param name="separator"></param>
        /// <returns></returns>
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
            sb.Append(TimestampUtc.ToString("O"));
            sb.Append(separator);
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
