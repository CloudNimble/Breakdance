using System;
using System.Diagnostics;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// 
    /// </summary>
    internal static class Ensure
    {

        /// <summary>
        /// Ensures that the specified argument is not null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        [DebuggerStepThrough]
        public static void ArgumentNotNull(object argument, string argumentName)
        {

#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(argument, argumentName);
#else
            if (argument is null)
            {
                throw new ArgumentNullException(argumentName);
            }
#endif

        }
    }

}
