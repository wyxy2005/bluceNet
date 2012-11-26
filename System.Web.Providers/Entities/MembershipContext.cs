namespace System.Web.Providers.Entities
{
    using System;
    using System.Data.Entity;
    using System.Runtime.CompilerServices;
    using System.Data.Entity.ModelConfiguration.Conventions;

    internal class MembershipContext : DbContext
    {
        public MembershipContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            if (modelBuilder != null)
            {
                modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            }
        }

        public DbSet<Application> Applications { get; set; }

        public DbSet<MembershipEntity> Memberships { get; set; }

        public DbSet<ProfileEntity> Profiles { get; set; }

        public DbSet<RoleEntity> Roles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<UsersInRole> UsersInRoles { get; set; }
    }
}

