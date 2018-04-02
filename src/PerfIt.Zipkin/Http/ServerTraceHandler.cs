using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing;
using Criteo.Profiling.Tracing.Transport;
using Criteo.Profiling.Tracing.Utils;

namespace PerfIt.Zipkin.Http
{
    public class ServerTraceHandler : DelegatingHandler
    {
        private readonly string _serviceName;
        private readonly ZipkinHttpTraceExtractor  _extractor = new ZipkinHttpTraceExtractor();
        private readonly ITraceInjector _injector = new ZipkinHttpTraceInjector();

        public ServerTraceHandler(string serviceName)
        {
            _serviceName = serviceName;
        }
       
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Trace trace;
            if (!_extractor.TryExtract(request.Headers, (c, key) => c.GetValues(key).FirstOrDefault(), out trace))
            {
                trace = Trace.Create();
            }
            
            Trace.Current = trace;
            using (new ServerTrace(_serviceName, request.Method.Method))
            {
                trace.Record(Annotations.Tag("http.host", request.RequestUri.Host .ToString()));
                trace.Record(Annotations.Tag("http.uri", request.RequestUri.ToString()));
                trace.Record(Annotations.Tag("http.path", request.RequestUri.AbsolutePath));
                
                var result = await base.SendAsync(request, cancellationToken);
                _injector.Inject(trace, result.Headers, (c, key, value) => c.Add(key, value));
                return result;
            }
        }
    }
}