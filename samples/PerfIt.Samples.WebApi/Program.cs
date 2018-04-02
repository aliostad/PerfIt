using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;

namespace PerfIt.Samples.WebApi
{
    class Program
    {
        /// <summary>
        /// This program hosts a Web API that has a controller decorated with a PerfIt filter and then
        /// sends an HTTP request to instrument.
        /// Since it is net452, it will raise ETW that can be captured by Enterprise Library. Note listener.LogToConsole();
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:34543/";
            var configuration = new HttpSelfHostConfiguration(baseAddress);
            configuration.Routes.Add("def", new HttpRoute("api/{controller}"));
            var server = new HttpSelfHostServer(configuration);
            server.OpenAsync().Wait();

            var listener = new ObservableEventListener();

            listener.EnableEvents(InstrumentationEventSource.Instance, EventLevel.LogAlways,
                Keywords.All);

            listener.LogToConsole();

            var client = new HttpClient();
            var result = client.GetAsync(baseAddress + "api/test").Result;
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);

            result.EnsureSuccessStatusCode();
            server.CloseAsync().Wait();

            Console.Read();
        }
    }
}
