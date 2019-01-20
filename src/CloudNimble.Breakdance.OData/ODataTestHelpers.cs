using CloudNimble.Breakdance.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CloudNimble.Breakdance.OData
{

    /// <summary>
    /// 
    /// </summary>
    public static class ODataTestHelpers
    {


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <returns></returns>
        //public static async Task<string> GetApiMetadata<T>(string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix) where T : ApiBase
        //{
        //    var response = await ExecuteTestRequest<T>(HttpMethod.Get, host, routeName, routePrefix, "/$metadata");
        //    var result = await response.Content.ReadAsStringAsync();
        //    var doc = XDocument.Parse(result);
        //    return doc.ToString();
        //}

    }
}
