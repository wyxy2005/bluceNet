namespace Clowa.EFProviders.ProviderSession
{
    using System;
    using System.Data.Entity;
    using System.Runtime.CompilerServices;
    using System.Data.Objects;

    internal class SessionContext :MemberShip
    {
        public SessionContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        { 
        }

        public ObjectQuery<Session> Sessions { get; set;}
    }
}
