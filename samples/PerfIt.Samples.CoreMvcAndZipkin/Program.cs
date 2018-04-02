using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using PerfIt.Zipkin;
using PerfIt.Zipkin.Http;
using PerfIt.Http;

namespace PerfIt.Samples.CoreMvcAndZipkin
{
    class Program
    {
        const string baseAddress = "http://localhost:34543/";

        /// <summary>
        /// This is sample whereby a .NET Core MVC API is hosted with a controller that is decorated with PerfIt filter.
        /// A Zipkin emitter is created with a Console dispatcher so that all Zipkin traces are sent to Console.
        /// A Zipkin tracer gets added by hooking into PerfItRuntime.InstrumentorCreated.
        /// On the other hand, we have HttpClient which gets a PerfIt handler with a ClientTraceHandler which injects Zipkin headers.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // server
            var h = BuildWebHost(args);
            h.Start();

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
                InnerHandler = new ClientTraceHandler("client-test", new HttpClientHandler())
            };

            var client = new HttpClient(handler);
            var result = client.GetAsync(baseAddress + "api/test").Result;
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);

            // notice Zipkin headers in the request as a result of ClientTraceHandler
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(result.RequestMessage.Headers.ToString());
            Console.ResetColor();
            result.EnsureSuccessStatusCode();

            Console.Read();

            h.StopAsync().Wait();
        }

        public static IWebHost BuildWebHost(string[] args) => WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseUrls(baseAddress)
            .Build();
    }

    class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "api",
                    defaults: new { action = "GET", controller = "test" },
                    template: "api/{controller}/{id?}");
            });

        }
    }
}

