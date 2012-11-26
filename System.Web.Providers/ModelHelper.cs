namespace System.Web.Providers
{
    using System;
    using System.Configuration;
    using System.Configuration.Provider;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Globalization;
    using System.Security;
    using System.Web.Hosting;
    using System.Web.Providers.Entities;
    using System.Web.Providers.Resources;

    internal static class ModelHelper
    {
        private static bool DbInitialized;
        private static bool MembershipInitialized;
        private static bool SessionInitialized;

        internal static Application CreateApplication(MembershipContext ctx, string appName)
        {
            Application entity = new Application {
                ApplicationId = Guid.NewGuid(),
                ApplicationName = appName
            };
            ctx.Applications.Add(entity);
            return entity;
        }

        internal static MembershipContext CreateMembershipContext(ConnectionStringSettings setting)
        {
            if (!DbInitialized)
            {
                DatabaseInitialize();
            }
            MembershipContext db = new MembershipContext(setting.Name);
            if (!MembershipInitialized)
            {
                EnsureDatabaseCreated(db);
                ExecuteSql(db, "CREATE NONCLUSTERED INDEX IDX_UserName ON Users (UserName)");
                MembershipInitialized = true;
            }
            return db;
        }

        internal static SessionContext CreateSessionContext(ConnectionStringSettings setting)
        {
            if (!DbInitialized)
            {
                DatabaseInitialize();
            }
            SessionContext db = new SessionContext(setting.Name);
            if (!SessionInitialized)
            {
                EnsureDatabaseCreated(db);
                ExecuteSql(db, "CREATE INDEX IX_Sessions_Expires ON Sessions (Expires)");
                SessionInitialized = true;
            }
            return db;
        }

        internal static User CreateUser(MembershipContext ctx, Guid id, string userName, Guid appId, bool isAnon)
        {
            User entity = new User {
                UserId = id,
                ApplicationId = appId,
                LastActivityDate = DateTime.UtcNow,
                UserName = userName,
                IsAnonymous = isAnon
            };
            ctx.Users.Add(entity);
            return entity;
        }

        private static void DatabaseInitialize()
        {
            Database.SetInitializer<SessionContext>(null);
            Database.SetInitializer<MembershipContext>(null);
            DbInitialized = true;
        }

        private static void EnsureDatabaseCreated(DbContext db)
        {
            if (db.Database.Exists())
            {
                string sql = ((IObjectContextAdapter) db).ObjectContext.CreateDatabaseScript();
                ExecuteSql(db, sql);
            }
            else
            {
                db.Database.Create();
            }
        }

        internal static int ExecuteSql(DbContext db, string sql)
        {
            string[] strArray = sql.Split(new char[] { ';' });
            int num = 0;
            foreach (string str in strArray)
            {
                try
                {
                    num += db.Database.ExecuteSqlCommand(str + ";", new object[0]);
                }
                catch
                {
                    return -1;
                }
            }
            return num;
        }

        internal static ConnectionStringSettings GetConnectionString(string connectionstringName)
        {
            if (string.IsNullOrEmpty(connectionstringName))
            {
                throw new ProviderException(ProviderResources.Connection_name_not_specified);
            }
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionstringName];
            if (settings == null)
            {
                throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Connection_string_not_found, new object[] { connectionstringName }));
            }
            return settings;
        }

        internal static string GetDefaultAppName()
        {
            try
            {
                string applicationVirtualPath = HostingEnvironment.ApplicationVirtualPath;
                if (!string.IsNullOrEmpty(applicationVirtualPath))
                {
                    return applicationVirtualPath;
                }
            }
            catch (SecurityException)
            {
            }
            return "/";
        }
    }
}

