using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using CloudNimble.Breakdance.AspNetCore;
using System.Net.Http;

namespace CloudNimble.Breakdance.Restier
{

    /// <summary>
    /// Helper methods for creating ResTIER APIs with AspNetCore.
    /// </summary>
    public static class RestierTestHelpers
    {
        /// <summary>
        /// Gets a new <see cref="TestServer" /> using the provided startup class in T.
        /// </summary>
        /// <returns>A new <see cref="TestServer" /> instance.</returns>
        public static TestServer GetTestableRestierServer<TApi, TContext>()
            where TApi : class
            where TContext : class
        {
            return AspNetCoreTestHelpers.GetTestableHttpServer(services =>
            {
                /* JHC Almost there...  maybe this needs to be in restier rather than in breakdance
                services.AddRestier((builder) =>
                {
                    // This delegate is executed after OData is added to the container.
                    // Add you replacement services here.
                    builder.AddRestierApi<TApi>(routeServices =>
                    {

                        routeServices
                            .AddEFCoreProviderServices<TContext>(opt => opt.UseSqlServer(Configuration.GetConnectionString("NorthwindEntities")))  // << JHC TODO: change this to use IConfiguration
                            .AddSingleton(new ODataValidationSettings
                            {
                                MaxTop = 5,
                                MaxAnyAllExpressionDepth = 3,
                                MaxExpansionDepth = 3,
                            });

                    });
                });
                */
            });

        }

    }
}
