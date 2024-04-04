using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace Breakdance.Tests.AspNetCore.Fakes
{

    /// <summary>
    /// A fake controller for testing the request/response pipeline in the <see cref="TestServer"/>.
    /// </summary>
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return Content("Hello world!");
        }

        [HttpPost]
        public ActionResult TestPost(string saySomething)
        {
            return Content($"You said, '{saySomething}'.");
        }

    }
}
