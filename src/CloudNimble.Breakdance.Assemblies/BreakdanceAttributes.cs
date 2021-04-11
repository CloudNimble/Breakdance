using System;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// Tells Breakdance that the attributed method generates a manifest file that is used to test functional outputs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class BreakdanceManifestGeneratorAttribute : Attribute
    {
    }

    /// <summary>
    /// Tells Breakdance that the attributed method generates a manifest file that is used to test functional outputs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class BreakdanceTestAssemblyAttribute : Attribute
    {
    }

}
