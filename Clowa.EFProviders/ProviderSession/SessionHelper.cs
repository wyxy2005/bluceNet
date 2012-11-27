using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Clowa.EFProviders.ProviderSession
{
    internal static class ModelHelper
    {
        private static bool SessionInitialized;

        internal static Application CreateApplication(EFProviders.MemberShip ms, string appName)
        {
            Application entity = new Application
            {
                Id = Guid.NewGuid(),
                Name = appName
            };
            ms.AddToApplication(entity);
            return entity;
        }

        internal static SessionContext CreateSessionContext(ConnectionStringSettings setting)
        { 
            SessionContext db = new SessionContext(setting.Name);
            if (!SessionInitialized)
            {
                EnsureDatabaseCreated(db);
                ExecuteSql(db, "CREATE INDEX IX_Sessions_Expires ON Sessions (Expires)");
                SessionInitialized = true;
            }
            return db;
        }

        private static void EnsureDatabaseCreated(MemberShip db)
        {
            if (db)
            {
                string sql = ((IObjectContextAdapter)db).ObjectContext.CreateDatabaseScript();
                ExecuteSql(db, sql);
            }
            else
            {
                db.Database.Create();
            }
        }
    }
}
