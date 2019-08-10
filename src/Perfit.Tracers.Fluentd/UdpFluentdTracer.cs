using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PerfIt.Tracers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Perfit.Tracers.Fluentd
{
    /// <summary>
    /// A tracer for fluentd using UDP transport
    /// </summary>
    public class UdpFluentdTracer : SimpleTracerBase
    {
        private readonly string _address;
        private readonly int _port;
        private readonly UdpClient _client = new UdpClient();
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            DateFormatString = "yyyy-MM-ddTHH:mm:ss.FFFK",
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Assumes the host and port exist in the environment variablse: 
        /// PERFIT_TRACER_FLUENTD_UDP_ADDRESS and PERFIT_TRACER_FLUENTD_UDP_PORT
        /// </summary>
        public UdpFluentdTracer()
        {
            var address = Environment.GetEnvironmentVariable("PERFIT_TRACER_FLUENTD_UDP_ADDRESS");
            var port = Environment.GetEnvironmentVariable("PERFIT_TRACER_FLUENTD_UDP_PORT");
            if (string.IsNullOrEmpty(address))
            {
                throw new InvalidOperationException("PERFIT_TRACER_FLUENTD_UDP_ADDRESS not set.");
            }

            if (string.IsNullOrEmpty(port))
            {
                throw new InvalidOperationException("PERFIT_TRACER_FLUENTD_UDP_PORT not set.");
            }

            _address = address;
            _port = int.Parse(port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address">hostname</param>
        /// <param name="port">The port Fluentd listens</param>
        public UdpFluentdTracer(string address, int port)
        {
            _port = port;
            _address = address;
        }

        /// <summary>
        /// Writes Tracedata
        /// </summary>
        /// <param name="data"></param>
        protected override void WriteTrace(TraceData data)
        {
            var s = JsonConvert.SerializeObject(data, _settings);
            var bb = Encoding.UTF8.GetBytes(s);
            _client.Send(bb, bb.Length, _address, _port);
        }
    }
}