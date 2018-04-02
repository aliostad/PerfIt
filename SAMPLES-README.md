PerfIt Samples
===

For ease of setup, most tracers below output to console. Of course, in production you would be outputing to a durable storage or an EventHub which later gets pushed to an analytic storage (eg Elasticsearch cluster).

## PerfIt.Samples.WebApi (net452)

This program hosts a Web API that has a controller decorated with a PerfIt filter and then sends an HTTP request to instrument.
Since it is net452, it will raise ETW that can be captured by Enterprise Library. Note `listener.LogToConsole();`

## PerfIt.Samples.CoreMvc (netcoreapp2.0)

This is sample whereby a .NET Core MVC API is hosted with a controller that is decorated with PerfIt filter. A Console tracer gets added by hooking into `PerfItRuntime.InstrumentorCreated` event.
On the other hand, we have HttpClient which gets a PerfIt handler with a console tracer.

## PerfIt.Samples.CoreMvcAndZipkin (netcoreapp2.0)

This is sample whereby a .NET Core MVC API is hosted with a controller that is decorated with PerfIt filter.
A Zipkin emitter is created with a Console dispatcher so that all Zipkin traces are sent to Console.
A Zipkin tracer gets added by hooking into PerfItRuntime.InstrumentorCreated.
On the other hand, we have HttpClient which gets a PerfIt handler with a `ClientTraceHandler` which injects Zipkin headers to the request.

## PerfIt.Samples.WebAPiAndZipkin (net452)

This program hosts a Web API that has a controller decorated with .a PerfIt filter and then sends an HTTP request to instrument
There is a Zipkin ServerTraceHandler to pick up headers from request and inject headers to the response.
Zipkin emitter has a console dispatcher which outputs spans to the console.
