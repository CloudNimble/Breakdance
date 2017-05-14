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

        public SportsDbContext(string connectionStringName)
        : base("name=" + connectionStringName)
        {
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }

        public virtual DbSet<Sport> Sports { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
        public virtual DbSet<Player> Players { get; set; }

    }
}
