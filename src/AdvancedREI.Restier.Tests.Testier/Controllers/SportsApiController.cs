using AdvancedREI.Restier.Tests.Testier.Model;
using Microsoft.Restier.Providers.EntityFramework;
using System;
using System.Linq;

namespace AdvancedREI.Restier.Tests.Testier.Controllers
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

    }

}