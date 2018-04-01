using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net.Http;
using PerfIt.Tracers;

namespace PerfIt.Samples.CoreMvc
{
    class Program
    {
        const string baseAddress = "http://localhost:34543/";

        class ConsoleTracer : SimpleTracerBase
        {
            protected override void WriteTrace(TraceData data)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Took {data.TimeTakenMilli}ms");
                Console.ResetColor();
            }
        }


        static void Main(string[] args)
        {

            // server
            var h = BuildWebHost(args);
            h.Start();


            // hook to the filter and add a tracer
            PerfItRuntime.InstrumentorCreated += (sender, e) => e.Instrumentor.Tracers.Add("Console", new ConsoleTracer());

            // call
            var client = new HttpClient();
            var result = client.GetAsync(baseAddress + "api/test").Result;
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);
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
