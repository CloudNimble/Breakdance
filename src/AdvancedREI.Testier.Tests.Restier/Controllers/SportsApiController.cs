using AdvancedREI.Testier.Tests.Restier.Model;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.Restier.Publishers.OData.Model;
using System;
using System.Linq;

namespace AdvancedREI.Testier.Tests.Restier.Controllers
{

    /// <summary>
    /// 
    /// </summary>
    public partial class SportsApi : EntityFrameworkApi<SportsDbContext>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SportsApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected internal bool CanInsertSports() => true;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="entitySet"></param>
        /// <returns></returns>
        protected internal IQueryable<Sport> OnFilterSport(IQueryable<Sport> entitySet)
        {
            return entitySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        protected internal void OnInsertedSports(Sport entity)
        {
        }

        [Operation]
        public string TestMethod()
        {
            return "Hi!";
        }

    }

}