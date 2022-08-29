using Microsoft.JSInterop;

namespace CloudNimble.Breakdance.Tests.Blazor.Models
{
    public class TestJavaScriptService
    {

        public IJSRuntime JSRuntime { get; internal set; }

        public TestJavaScriptService(IJSRuntime runtime)
        {
            JSRuntime = runtime;
        }

    }

}
