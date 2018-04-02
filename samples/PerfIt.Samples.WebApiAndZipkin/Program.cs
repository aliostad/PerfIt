using PerfIt.Http;
using PerfIt.Zipkin;
using PerfIt.Zipkin.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;

namespace PerfIt.Samples.WebApiAndZipkin
{
    class Program
    {
        /// <summary>
        /// This program hosts a Web API that has a controller decorated with .a PerfIt filter and then sends an HTTP request to instrument
        /// There is a Zipkin ServerTraceHandler to pick up headers from request and inject headers to the response.
        /// Zipkin emitter has a console dispatcher which outputs spans to the console.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:34543/";
            var configuration = new HttpSelfHostConfiguration(baseAddress);
            configuration.Routes.Add("def", new HttpRoute("api/{controller}"));
            configuration.MessageHandlers.Add(new ServerTraceHandler("server-test")); // adding Zipkin handler to inject headers
            var server = new HttpSelfHostServer(configuration);
            server.OpenAsync().Wait();

            // zipkin emitter 
            var emitter = new SimpleEmitter();
            emitter.RegisterDispatcher(new ConsoleDispatcher());

            // hook to the filter and add a tracer
            PerfItRuntime.InstrumentorCreated += (sender, e) =>
            {
                if (e.Info.CategoryName == "server-test")
                    e.Instrumentor.Tracers.Add("Console", new ServerTracer(emitter));
            };

            // handler
            var handler = new PerfitClientDelegatingHandler("client-test", new ClientTracer(emitter))
            {
                InnerHandler = new ClientTraceHandler("client-test", new HttpClientHandler()),
                PublishCounters = false
            };

            var client = new HttpClient(handler);
            var result = client.GetAsync(baseAddress + "api/test").Result;
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);

            // notice Zipkin headers in the request as a result of ClientTraceHandler
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(result.RequestMessage.Headers.ToString());
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Headers.ToString());
            Console.ResetColor();
            result.EnsureSuccessStatusCode();

            Console.Read();
        }
    }
}
