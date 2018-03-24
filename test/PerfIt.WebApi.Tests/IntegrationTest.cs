using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;
using Xunit;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace PerfIt.WebApi.Tests
{
    public class IntegrationTest
    {

        // this category must have been installed for the tests to run successfully
        private const string TestCategory = "PerfItTests";

        public IntegrationTest()
        {
            CounterInstaller.InstallStandardCounters(TestCategory);
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

        [Fact]
        public void Server_CanServe()
        {
            string baseAddress = "http://localhost:34543/";
            var configuration = new HttpSelfHostConfiguration(baseAddress);
            configuration.Routes.Add("def", new HttpRoute("api/{controller}"));
            var server = new HttpSelfHostServer(configuration);
            server.OpenAsync().Wait();
            var client = new HttpClient();
            var result = client.GetAsync(baseAddress + "api/test").Result;
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);

            result.EnsureSuccessStatusCode();
            server.CloseAsync().Wait();

        }

        [Fact(Skip = "Requires admin")]
        public void InstallWillInstallTheCategoryAndUseCatProvidedForTheNullOne()
        {
            CounterInstaller.Install(Assembly.GetExecutingAssembly(), new FilterDiscoverer(), "Woohooo");
        }

    }



    public class TestController : ApiController
    {
        [PerfItFilter("PerfItTests",
            Counters = new[] {CounterTypes.AverageTimeTaken, CounterTypes.LastOperationExecutionTime, CounterTypes.NumberOfOperationsPerSecond, CounterTypes.TotalNoOfOperations},
            InstanceName = "Washah",
            RaisePublishErrors = true)]
        public string Get()
        {
            return Guid.NewGuid().ToString();
        }
    }

    public class TestController2 : ApiController
    {
        [PerfItFilter(null,
            Counters = new[] { CounterTypes.AverageTimeTaken, CounterTypes.LastOperationExecutionTime, CounterTypes.NumberOfOperationsPerSecond, CounterTypes.TotalNoOfOperations },
            InstanceName = "Washah",
            RaisePublishErrors = true)]
        public string Get()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
