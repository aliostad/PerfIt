namespace PerfIt
{
    /// <summary>
    /// PublishInstrumentationDelegate callback definition.
    /// </summary>
    /// <param name="categoryName"></param>
    /// <param name="instanceName"></param>
    /// <param name="elapsedMilliseconds"></param>
    /// <param name="instrumentationContext"></param>
    public delegate void PublishInstrumentationDelegate(string categoryName, string instanceName,
        double elapsedMilliseconds, string instrumentationContext);
}
