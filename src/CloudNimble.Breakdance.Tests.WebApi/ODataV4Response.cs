using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breakdance.Tests.WebApi
{
    public class ODataV4Response<T>
    {

        [JsonProperty("value")]
        public List<T> Items { get; set; }

    }
}
