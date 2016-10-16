using System;

namespace PerfIt.Castle.Interception
{
    /// <summary>
    /// Provides a means of providing a <see cref="SimpleInstrumentor"/> to the rest of the
    /// system. A default instance is provided given the configured <see cref="Info"/>.
    /// </summary>
    public class InstrumentorRequiredEventArgs : EventArgs
    {
        public string CategoryName { get; private set; }

        public IInstrumentationInfo Info { get; private set; }

        private Lazy<SimpleInstrumentor> _lazyInstrumentor;

        private SimpleInstrumentor GetDefaultInstrumentor()
        {
            return new SimpleInstrumentor(Info);
        }

        public SimpleInstrumentor Instrumentor
        {
            get { return _lazyInstrumentor.Value; }
            set { _lazyInstrumentor = new Lazy<SimpleInstrumentor>(() => value ?? GetDefaultInstrumentor()); }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        internal InstrumentorRequiredEventArgs(string categoryName, IInstrumentationInfo info)
        {
            CategoryName = categoryName;
            // TODO: TBD: ICloneable would be better but for potential snag with the custom Attributes
            Info = new InstrumentationInfo(info);
            _lazyInstrumentor = new Lazy<SimpleInstrumentor>(GetDefaultInstrumentor);
        }
    }
}
