using System;

namespace CloudNimble.Breakdance.Tests.Assemblies.SampleApis
{
    public class SomeEventArgs
    {

        public SomeEventArgs(string s) => Text = s;

        public String Text { get; private set; }

    }

}