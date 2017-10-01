using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.SampleApis
{
    public class SomeEventArgs
    {

        public SomeEventArgs(string s) => Text = s;

        public String Text { get; private set; }

    }

}