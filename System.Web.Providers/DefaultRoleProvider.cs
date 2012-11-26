namespace System.Web.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Configuration.Provider;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Web.Providers.Entities;
    using System.Web.Providers.Resources;
    using System.Web.Security;

    public class DefaultRoleProvider : RoleProvider
    {
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            if (usernames == null)
            {
                throw new ArgumentNullException("usernames");
            }
            if (roleNames == null)
            {
                throw new ArgumentNullException("roleNames");
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                List<User> list = new List<User>();
                foreach (string str in usernames)
                {
                    User item = QueryHelper.GetUser(context, str, this.ApplicationName);
                    if (item == null)
                    {
                        throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_this_user_not_found, new object[] { str }));
                    }
                    list.Add(item);
                }
                List<RoleEntity> list2 = new List<RoleEntity>();
                foreach (string str2 in roleNames)
                {
                    RoleEntity entity = QueryHelper.GetRole(context, str2, this.ApplicationName);
                    if (entity == null)
                    {
                        throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_role_not_found, new object[] { str2 }));
                    }
                    list2.Add(entity);
                }
                foreach (User user2 in list)
                {
                    foreach (RoleEntity entity2 in list2)
                    {
                        if (QueryHelper.GetUserInRole(context, user2.UserId, entity2.RoleId) != null)
                        {
                            throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_this_user_already_in_role, new object[] { user2.UserName, entity2.RoleName }));
                        }
                        UsersInRole role2 = new UsersInRole {
                            UserId = user2.UserId,
                            RoleId = entity2.RoleId
                        };
                        context.UsersInRoles.Add(role2);
                    }
                }
                context.SaveChanges();
            }
        }

        public override void CreateRole(string roleName)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "roleName";
            Exception exception = ValidationHelper.CheckParameter(ref roleName, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                bool createIfNotExist = true;
                Application application = QueryHelper.GetApplication(context, this.ApplicationName, createIfNotExist);
                if (QueryHelper.GetRole(context, roleName, application.ApplicationId) != null)
                {
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_role_already_exists, new object[] { roleName }));
                }
                RoleEntity entity = new RoleEntity {
                    RoleId = Guid.NewGuid(),
                    ApplicationId = application.ApplicationId,
                    RoleName = roleName
                };
                context.Roles.Add(entity);
                context.SaveChanges();
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "roleName";
            Exception exception = ValidationHelper.CheckParameter(ref roleName, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                RoleEntity entity = QueryHelper.GetRole(context, roleName, this.ApplicationName);
                if (entity == null)
                {
                    return false;
                }
                IQueryable<UsersInRole> source = from usr in context.UsersInRoles
                    join r in context.Roles on usr.RoleId equals r.RoleId into r
                    join a in context.Applications on r.ApplicationId equals a.ApplicationId into a
                    where (a.ApplicationName.ToLower() == this.ApplicationName.ToLower()) && (roleName.ToLower() == r.RoleName)
                    select usr;
                if (throwOnPopulatedRole && (source.Count<UsersInRole>() > 0))
                {
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Role_is_not_empty, new object[0]));
                }
                foreach (UsersInRole role in source)
                {
                    context.UsersInRoles.Remove(role);
                }
                context.Roles.Remove(entity);
                context.SaveChanges();
                return true;
            }
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            //bool checkForNull = true;
            //bool checkIfEmpty = true;
            //bool checkForCommas = false;
            //int maxSize = 0x100;
            //string paramName = "usernameToMatch";
            //Exception exception = ValidationHelper.CheckParameter(ref usernameToMatch, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            //if (exception != null)
            //{
            //    throw exception;
            //}
            //bool flag4 = true;
            //bool flag5 = true;
            //bool flag6 = true;
            //int num2 = 0x100;
            //string str2 = "roleName";
            //exception = ValidationHelper.CheckParameter(ref roleName, flag4, flag5, flag6, num2, str2);
            //if (exception != null)
            //{
            //    throw exception;
            //}
            //using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            //{
            //    if (QueryHelper.GetRole(context, roleName, this.ApplicationName) == null)
            //    {
            //        throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_role_not_found, new object[] { roleName }));
            //    }
            //    return (from usr in context.UsersInRoles
            //        join r in context.Roles on usr.RoleId equals r.RoleId into r
            //        join a in context.Applications on r.ApplicationId equals a.ApplicationId into a
            //        where ((a.ApplicationName.ToLower() == this.ApplicationName.ToLower()) && usr.User.UserName.Contains(usernameToMatch)) && (r.RoleName.ToLower() == roleName.ToLower())
            //        orderby <>h__TransparentIdentifier5.<>h__TransparentIdentifier4.usr.User.UserName
            //        select usr.User.UserName).ToArray<string>();
            //}
            return null;
        }

        public override string[] GetAllRoles()
        {
            //using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            //{
            //    return (from r in context.Roles
            //        join a in context.Applications on r.ApplicationId equals a.ApplicationId into a
            //        where a.ApplicationName.ToLower() == this.ApplicationName.ToLower()
            //        orderby <>h__TransparentIdentifier8.r.RoleName
            //        select r.RoleName).ToArray<string>();
            //}
            return null;
        }

        public override string[] GetRolesForUser(string username)
        {
            bool checkForNull = true;
            bool checkIfEmpty = false;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "username";
            Exception exception = ValidationHelper.CheckParameter(ref username, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            if (username.Length < 1)
            {
                return new string[0];
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return QueryHelper.GetRolesNamesForUser(context, this.ApplicationName, username);
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            //bool checkForNull = true;
            //bool checkIfEmpty = true;
            //bool checkForCommas = true;
            //int maxSize = 0x100;
            //string paramName = "roleName";
            //Exception exception = ValidationHelper.CheckParameter(ref roleName, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            //if (exception != null)
            //{
            //    throw exception;
            //}
            //using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            //{
            //    return (from usr in context.UsersInRoles
            //        join r in context.Roles on usr.RoleId equals r.RoleId into r
            //        join a in context.Applications on r.ApplicationId equals a.ApplicationId into a
            //        where (a.ApplicationName.ToLower() == this.ApplicationName.ToLower()) && (r.RoleName.ToLower() == roleName.ToLower())
            //        orderby <>h__TransparentIdentifiera.<>h__TransparentIdentifier9.usr.User.UserName
            //        select usr.User.UserName).ToArray<string>();
            //}
            return null;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (string.IsNullOrEmpty(name))
            {
                name = "DefaultRoleProvider";
            }
            this.ConnectionString = ModelHelper.GetConnectionString(config["connectionStringName"]);
            config.Remove("connectionStringName");
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", string.Format(CultureInfo.CurrentCulture, ProviderResources.RoleProvider_description, new object[0]));
            }
            base.Initialize(name, config);
            if (!string.IsNullOrEmpty(config["applicationName"]))
            {
                this.ApplicationName = config["applicationName"];
            }
            else
            {
                this.ApplicationName = ModelHelper.GetDefaultAppName();
            }
            config.Remove("applicationName");
            if (config.Count > 0)
            {
                string key = config.GetKey(0);
                if (!string.IsNullOrEmpty(key))
                {
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_unrecognized_attribute, new object[] { key }));
                }
            }
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            bool checkForNull = true;
            bool checkIfEmpty = false;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "username";
            Exception exception = ValidationHelper.CheckParameter(ref username, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            bool flag5 = true;
            bool flag6 = true;
            bool flag7 = true;
            int num2 = 0x100;
            string str2 = "roleName";
            exception = ValidationHelper.CheckParameter(ref roleName, flag5, flag6, flag7, num2, str2);
            if (exception != null)
            {
                throw exception;
            }
            if (username.Length < 1)
            {
                return false;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return ((from usr in context.UsersInRoles
                    join r in context.Roles on usr.RoleId equals r.RoleId into r
                    join a in context.Applications on r.ApplicationId equals a.ApplicationId into a
                    join u in context.Users on usr.UserId equals u.UserId into u
                    where ((a.ApplicationName.ToLower() == this.ApplicationName.ToLower()) && (username.ToLower() == u.UserName.ToLower())) && (roleName.ToLower() == r.RoleName.ToLower())
                    select usr).FirstOrDefault<UsersInRole>() != null);
            }
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            if (usernames == null)
            {
                throw new ArgumentNullException("usernames");
            }
            if (roleNames == null)
            {
                throw new ArgumentNullException("roleNames");
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                List<User> list = new List<User>();
                foreach (string str in usernames)
                {
                    User item = QueryHelper.GetUser(context, str, this.ApplicationName);
                    if (item == null)
                    {
                        throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_this_user_not_found, new object[] { str }));
                    }
                    list.Add(item);
                }
                List<RoleEntity> list2 = new List<RoleEntity>();
                foreach (string str2 in roleNames)
                {
                    RoleEntity entity = QueryHelper.GetRole(context, str2, this.ApplicationName);
                    if (entity == null)
                    {
                        throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_role_not_found, new object[] { str2 }));
                    }
                    list2.Add(entity);
                }
                foreach (User user2 in list)
                {
                    foreach (RoleEntity entity2 in list2)
                    {
                        UsersInRole role = QueryHelper.GetUserInRole(context, user2.UserId, entity2.RoleId);
                        if (role == null)
                        {
                            throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_this_user_already_not_in_role, new object[] { user2.UserName, entity2.RoleName }));
                        }
                        context.UsersInRoles.Remove(role);
                    }
                }
                context.SaveChanges();
            }
        }

        public override bool RoleExists(string roleName)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "roleName";
            Exception exception = ValidationHelper.CheckParameter(ref roleName, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return (QueryHelper.GetRole(context, roleName, this.ApplicationName) != null);
            }
        }

        public override string ApplicationName { get; set; }

        private ConnectionStringSettings ConnectionString { get; set; }
    }
}

