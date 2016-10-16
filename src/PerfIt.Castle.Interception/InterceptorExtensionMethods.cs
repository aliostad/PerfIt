using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace PerfIt.Castle.Interception
{
    // TODO: TBD: it is internal here; should it be public?
    internal static class InterceptorExtensionMethods
    {
        /// <summary>
        /// Provides Asynchronous syntactic sugar on <see cref="IInvocation.Proceed"/>.
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        internal static Task ProceedAsync(this IInvocation invocation)
        {
            return Task.Run(() => invocation.Proceed());
        }
    }
}
