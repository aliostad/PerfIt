using System;
using Xunit;

namespace PerfIt.Castle.Interception.Tests
{
    internal static class XunitExtensionMethods
    {
        private const double DefaultEpsilon = 0.001d;

        internal static void AssertWithin(this double a, double b, string message, double epsilon = DefaultEpsilon)
        {
            Assert.True(Math.Abs(a - b) < epsilon, message);
        }

        internal static void AssertGreaterThan(this double a, double b, string message, double epsilon = DefaultEpsilon)
        {
            Assert.True(a > b - epsilon, message);
        }

        internal static void AssertGreaterThanOrEqual(this double a, double b, string message, double epsilon = DefaultEpsilon)
        {
            Assert.True(a >= b - epsilon, message);
        }
    }
}
