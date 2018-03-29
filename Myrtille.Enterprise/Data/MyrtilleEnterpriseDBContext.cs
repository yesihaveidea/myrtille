using System.Data.Entity;
using System.Data.SqlServerCe;

namespace Myrtille.Enterprise
{
    public class MyrtilleEnterpriseDBContext : DbContext
    {
        public MyrtilleEnterpriseDBContext() : base(new SqlCeConnection(@"Data Source=|DataDirectory|MyrtilleEnterprise.sdf;Persist Security Info=False;"), contextOwnsConnection: true)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyrtilleEnterpriseDBContext, MigrationConfiguration>());
        }

        public virtual DbSet<Session> Session { get; set; }
        public virtual DbSet<SessionGroup> SessionGroup { get; set; }
        public virtual DbSet<Host> Host { get; set; }
        public virtual DbSet<HostAccessGroups> HostAccessGroups { get; set; }
    }
}