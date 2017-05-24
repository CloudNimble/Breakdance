using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace AdvancedREI.Restier.Tests.Testier.Model
{

    public partial class SportsDbContext : DbContext
    {
        public SportsDbContext()
            : base("name=SportsDbContext")
        {
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;
        }


        public virtual DbSet<Sport> Sports { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
        public virtual DbSet<Player> Players { get; set; }

    }
}
