using Newtonsoft.Json;
using PerfIt;
using PerfIt.Tracers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Perfit.Tracers.Fluentd.Tests
{

    public class UdpListener : IDisposable
    {
        private readonly UdpClient _listener;
        private readonly IPEndPoint _endpoint;
        private readonly CancellationTokenSource _source = new CancellationTokenSource(); 

        public UdpListener(int port)
        {
            _listener = new UdpClient(port);
            _endpoint = new IPEndPoint(IPAddress.Any, port);
            ReceiveAsync(_source.Token); // dont wait
        }

        private async Task ReceiveAsync(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                try
                {
                    LastResult = await _listener.ReceiveAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public UdpReceiveResult LastResult { get; private set; }

        public void Dispose()
        {
            _source.Cancel();
            _listener.Close();
        }
    } 

    public class IntegrationTest : IDisposable
    {
        const int port = 21969;

        private readonly UdpListener _listener = new UdpListener(port);

        [Fact]
        public void CanSend()
        {
            var trc = new UdpFluentdTracer("localhost", port);
            var cor = "ha ha";
            var ii = new InstrumentationInfo()
            {
                CategoryName = "cat",
                CorrelationIdKey = Guid.NewGuid().ToString(),
                InstanceName = "ins",
                Name = "subo"
            };
            var token = trc.Start(ii);
            trc.Finish(token, 100, correlationId: cor);
            Thread.Sleep(1000);
            Assert.NotNull(_listener.LastResult.Buffer);
            var s = Encoding.UTF8.GetString(_listener.LastResult.Buffer);
            var data = JsonConvert.DeserializeObject<TraceData>(s);

        }
        
        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
