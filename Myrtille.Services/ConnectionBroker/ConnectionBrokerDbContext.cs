using System.Data.Entity;

namespace Myrtille.Services.ConnectionBroker
{
    public partial class ConnectionBrokerDbContext : DbContext
    {
        public ConnectionBrokerDbContext()
            : base("name=ConnectionBrokerDbContext")
        {
        }

        public virtual DbSet<Server> Server { get; set; }
        public virtual DbSet<Session> Session { get; set; }
        public virtual DbSet<Target> Target { get; set; }
        public virtual DbSet<TargetIp> TargetIp { get; set; }
        public virtual DbSet<TargetProperty> TargetProperty { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(e => e.Session)
                .WithOptional(e => e.User)
                .WillCascadeOnDelete();
        }
    }
}