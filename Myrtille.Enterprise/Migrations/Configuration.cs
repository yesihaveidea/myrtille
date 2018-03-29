using System.Data.Entity.Migrations;

namespace Myrtille.Enterprise.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<MyrtilleEnterpriseDBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;

            //var migrator = new DbMigrator(this);
            //if (migrator.GetPendingMigrations().Any())
            //{
            //    migrator.Update();
            //}
        }

        protected override void Seed(MyrtilleEnterpriseDBContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}