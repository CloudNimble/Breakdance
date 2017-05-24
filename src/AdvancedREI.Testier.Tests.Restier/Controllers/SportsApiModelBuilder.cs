using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;

namespace AdvancedREI.Testier.Tests.Restier.Model
{

    /// <summary>
    /// 
    /// </summary>
    public class SportsApiModelBuilder : IModelBuilder
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            var modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.EntitySet<Sport>("Sports");
            var teams = modelBuilder.EntitySet<Team>("Teams");
            teams.EntityType.Action("EntitySetAction");
            modelBuilder.EntitySet<Player>("Players");
            modelBuilder.Action("TestMethod").Returns<string>();
            return Task.FromResult(modelBuilder.GetEdmModel());
        }

    }

}