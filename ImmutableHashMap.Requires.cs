using System;
using System.Diagnostics;

namespace RoslynImmutableHashMapConsoleApp
{
    internal sealed partial class ImmutableHashMap<TKey, TValue>
    {
        private static class Requires
        {
            [DebuggerStepThrough]
            public static T NotNullAllowStructs<T>(T value, string parameterName)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            [DebuggerStepThrough]
            public static T NotNull<T>(T value, string parameterName) where T : class
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            [DebuggerStepThrough]
            public static Exception FailRange(string parameterName, string message = null)
            {
                if (string.IsNullOrEmpty(message))
                {
                    throw new ArgumentOutOfRangeException(parameterName);
                }

                throw new ArgumentOutOfRangeException(parameterName, message);
            }

            [DebuggerStepThrough]
            public static void Range(bool condition, string parameterName, string message = null)
            {
                if (!condition)
                {
                    Requires.FailRange(parameterName, message);
                }
            }
        }
    }
}