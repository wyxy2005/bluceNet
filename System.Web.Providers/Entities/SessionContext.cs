namespace System.Web.Providers.Entities
{
    using System;
    using System.Data.Entity;
    using System.Runtime.CompilerServices;

    internal class SessionContext : DbContext
    {
        public SessionContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        public DbSet<Session> Sessions { get; set; }
    }
}

