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

         private const string TestCategory = "PerfItTests";

        public IntegrationTest()
        {
            PerfItRuntime.InstallStandardCounters(TestCategory);
        }

        [Fact]
        public void ClientHandler_CanCallGoogle()
        {
            var client = new HttpClient(new PerfitClientDelegatingHandler(TestCategory)
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
