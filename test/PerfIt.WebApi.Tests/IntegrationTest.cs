﻿using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
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

        [Fact]
        public void Server_CanServe()
        {
            var listener = ConsoleLog.CreateListener();
            listener.EnableEvents(InstrumentationEventSource.Instance, EventLevel.LogAlways,
                Keywords.All);

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

        [Fact]
        public void InstallWillInstallTheCategoryAndUseCatProvidedForTheNullOne()
        {
            PerfItRuntime.Install(Assembly.GetExecutingAssembly(), new FilterDiscoverer(), "Woohooo");
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
