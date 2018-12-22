using CloudNimble.Breakdance.Tests.Restier.Model;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.EntityFramework;
using System;
using System.Linq;

namespace CloudNimble.Breakdance.Tests.Restier.Controllers
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