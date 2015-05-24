using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PerfIt.WebApi.Tests
{
    public class IntegrationTest
    {

        [Fact]
        public void ClientHandler_CanCallGoogle()
        {
            var client = new HttpClient(new PerfitClientDelegatingHandler("PerfItTest45")
            {
                InnerHandler = new HttpClientHandler(),
                RaisePublishErrors = true
            });

            for (int i = 0; i < 10; i++)
            {
                var response = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://google.com")).Result;                
            }
        }
    }
}
