using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Hosting;
using Clowa.EFProviders.ProviderSecurity;
using System.Configuration.Provider;
using System.Linq.Expressions;
using System.Diagnostics;

namespace Clowa.EFProviders.ProviderSecurity
{
    /// <summary>
    /// Custom role provider using the Entity Framework.
    /// </summary>
    public class EFRoleProvider : RoleProvider
    {
        #region members
        private const string EVENTSOURCE = "EFRoleProvider";
        private const string EVENTLOG = "Application";
        private const string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private string connectionString;
        #endregion

        #region properties
        /// <summary>
        /// Gets or sets the name of the application to store and retrieve role information for.
        /// </summary>
        /// <returns>
        /// The name of the application to store and retrieve role information for.
        /// </returns>
        public override string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [write exceptions to event log].
        /// </summary>
        /// <value><c>true</c> if [write exceptions to event log]; otherwise, <c>false</c>.
        /// </value>
        public bool WriteExceptionsToEventLog { get; set; }
        #endregion

        #region public methods
        /// <summary>
        /// System.Configuration.Provider.ProviderBase.Initialize Method
        /// </summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The name of the provider is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The name of the provider has a length of zero.
        /// </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// An attempt is made to call <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> on a provider after the provider has already been initialized.
        /// </exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            // Initialize values from web.config.
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (String.IsNullOrEmpty(name))
            {
                name = "EFRoleProvider";
            }

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Smart-Soft EF Role Provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            ApplicationName = (string)ProviderUtils.GetConfigValue(config, "applicationName", HostingEnvironment.ApplicationVirtualPath);
            WriteExceptionsToEventLog = (bool)ProviderUtils.GetConfigValue(config, "writeExceptionsToEventLog", false);

            // Read connection string.
            ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[config["connectionStringName"]];

            if (connectionStringSettings == null || connectionStringSettings.ConnectionString.Trim() == string.Empty)
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = connectionStringSettings.ConnectionString;
        }

        /// <summary>
        /// Gets a value indicating whether the specified user is in the specified role for the configured applicationName.
        /// </summary>
        /// <param name="username">The user name to search for.</param>
        /// <param name="roleName">The role to search in.</param>
        /// <returns>true if the specified user is in the specified role for the configured applicationName; otherwise, false.</returns>
        public override bool IsUserInRole(string username, string roleName)
        {
            try
            {
                using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
                {
                    if (!EFMembershipProvider.CheckUser(username, ApplicationName, context))
                    {
                        return false;
                    }

                    return (from u in context.User
                            where u.Username == username && u.Application.Name == ApplicationName
                            from r in u.Role
                            where r.Name == roleName && r.Application.Name == ApplicationName
                            select r).Count() > 0;
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "IsUserInRole");
                }

                throw;
            }
        }

        /// <summary>
        /// Gets a list of the roles that a specified user is in for the configured applicationName.
        /// </summary>
        /// <param name="username">The user to return a list of roles for.</param>
        /// <returns>A string array containing the names of all the roles that the specified user is in for the configured applicationName.</returns>
        public override string[] GetRolesForUser(string username)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                if (!EFMembershipProvider.CheckUser(username, ApplicationName, context))
                {
                    throw new ArgumentNullException("username");
                }

                return (from u in context.User
                        where u.Username == username && u.Application.Name == ApplicationName
                        from r in u.Role
                        where r.Application.Name == ApplicationName
                        select r.Name).ToArray();
            }
        }

        /// <summary>
        /// Adds a new role to the data source for the configured applicationName.
        /// </summary>
        /// <param name="roleName">The name of the role to create.</param>
        public override void CreateRole(string roleName)
        {
            // Validate role name
            if (roleName.Contains(","))
            {
                throw new ArgumentException("Role names cannot contain commas.");
            }

            if (RoleExists(roleName))
            {
                throw new ProviderException("Role name already exists.");
            }

            try
            {
                using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
                {
                    Application application = ProviderUtils.EnsureApplication(ApplicationName, context);

                    // Create new role
                    Role newRole = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        Application = application
                    };
                    context.AddToRole(newRole);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "CreateRole");
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Removes a role from the data source for the configured applicationName.
        /// </summary>
        /// <param name="roleName">The name of the role to delete.</param>
        /// <param name="throwOnPopulatedRole">If true, throw an exception if <paramref name="roleName"/> has one or more members and do not delete <paramref name="roleName"/>.</param>
        /// <returns>
        /// true if the role was successfully deleted; otherwise, false.
        /// </returns>
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            // Validate role
            if (!RoleExists(roleName))
            {
                throw new ProviderException("Role does not exist.");
            }

            if (throwOnPopulatedRole && GetUsersInRole(roleName).Length > 0)
            {
                throw new ProviderException("Cannot delete a populated role.");
            }

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                Role role = GetRole(r => r.Name == roleName, context);
                if (role == null)
                {
                    return false;
                }

                try
                {
                    context.DeleteObject(role);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    if (WriteExceptionsToEventLog)
                    {
                        WriteToEventLog(ex, "DeleteRole");
                        return false;
                    }

                    throw;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified role name already exists in the role data source for the configured applicationName.
        /// </summary>
        /// <returns>true if the role name already exists in the data source for the configured applicationName; otherwise, false.</returns>
        /// <param name="roleName">The name of the role to search for in the data source.</param>
        public override bool RoleExists(string roleName)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                try
                {
                    return GetRole(r => r.Name == roleName, context) != null;
                }
                catch (ProviderException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Adds the specified user names to the specified roles for the configured applicationName.
        /// </summary>
        /// <param name="userNames">A string array of user names to be added to the specified roles.</param>
        /// <param name="roleNames">A string array of the role names to add the specified user names to.</param>
        public override void AddUsersToRoles(string[] userNames, string[] roleNames)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<Role> roles = context.Role.Where(MatchRoleApplication()).Where(ProviderUtils.BuildContainsExpression<Role, string>(r => r.Name, roleNames));
                if (roles.Count() != roleNames.Length)
                {
                    throw new ProviderException("Role not found.");
                }

                IQueryable<User> users = context.User.Where(MatchUserApplication()).Where(ProviderUtils.BuildContainsExpression<User, string>(u => u.Username, userNames));
                if (users.Count() != userNames.Length)
                {
                    throw new ProviderException("User not found.");
                }

                try
                {
                    foreach (User user in users)
                    {
                        foreach (Role role in roles)
                        {
                            // Check whether user is already in role
                            if (IsUserInRole(user.Username, role.Name))
                            {
                                throw new ProviderException(string.Format("User is already in role '{0}'.", role.Name));
                            }

                            user.Role.Add(role);
                        }
                    }

                    context.SaveChanges(false);
                }
                catch (Exception ex)
                {
                    if (WriteExceptionsToEventLog)
                    {
                        WriteToEventLog(ex, "AddUsersToRoles");
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    context.Connection.Close();
                }
            }
        }

        /// <summary>
        /// Removes the specified user names from the specified roles for the configured applicationName.
        /// </summary>
        /// <param name="userNames">A string array of user names to be removed from the specified roles.</param>
        /// <param name="roleNames">A string array of role names to remove the specified user names from.</param>
        public override void RemoveUsersFromRoles(string[] userNames, string[] roleNames)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<Role> roles = context.Role.Where(MatchRoleApplication()).Where(ProviderUtils.BuildContainsExpression<Role, string>(r => r.Name, roleNames));
                if (roles.Count() != roleNames.Length)
                {
                    throw new ProviderException("Role not found.");
                }

                IQueryable<User> users = context.User.Include("Role").Where(MatchUserApplication()).Where(ProviderUtils.BuildContainsExpression<User, string>(u => u.Username, userNames));
                if (users.Count() != userNames.Length)
                {
                    throw new ProviderException("User not found.");
                }

                try
                {
                    foreach (User user in users)
                    {
                        foreach (Role role in roles)
                        {
                            /*if (!user.Role.IsLoaded)
                            {
                                user.Role.Load();
                            }*/

                            if (user.Role.Contains(role))
                            {
                                user.Role.Remove(role);
                            }
                        }
                    }

                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    if (WriteExceptionsToEventLog)
                    {
                        WriteToEventLog(ex, "RemoveUsersFromRoles");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of users in the specified role for the configured applicationName.
        /// </summary>
        /// <param name="roleName">The name of the role to get the list of users for.</param>
        /// <returns>
        /// A string array containing the names of all the users who are members of the specified role for the configured applicationName.
        /// </returns>
        public override string[] GetUsersInRole(string roleName)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                Role role = GetRole(r => r.Name == roleName, context);
                if (role == null)
                {
                    throw new ProviderException("Role not found.");
                }

                if (!role.User.IsLoaded)
                {
                    role.User.Load();
                }

                return role.User.Select(u => u.Name).ToArray();
            }
        }

        /// <summary>
        /// Gets a list of all the roles for the configured applicationName.
        /// </summary>
        /// <returns>A string array containing the names of all the roles stored in the data source for the configured applicationName.</returns>
        public override string[] GetAllRoles()
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                return context.Role.Where(MatchRoleApplication()).Select(r => r.Name).ToArray();
            }
        }

        /// <summary>
        /// Gets an array of user names in a role where the user name contains the specified user name to match.
        /// </summary>
        /// <returns>A string array containing the names of all the users where the user name matches <paramref name="usernameToMatch" /> 
        /// and the user is a member of the specified role.</returns>
        /// <param name="roleName">The role to search in.</param>
        /// <param name="usernameToMatch">The user name to search for.</param>
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                Role role = GetRole(r => r.Name == roleName, context);
                if (role == null)
                {
                    throw new ProviderException("Role not found.");
                }

                if (!role.User.IsLoaded)
                {
                    role.User.Load();
                }

                return role.User.Select(u => u.Username).Where(un => un.Contains(usernameToMatch)).ToArray();
            }
        }

        #endregion

        #region private methods
        /// <summary>
        /// Get role from database. Throws an error if the role could not be found.
        /// </summary>
        /// <param name="query">The role query.</param>
        /// <param name="context">The context.</param>
        /// <returns>Found role entity.</returns>
        private Role GetRole(Expression<Func<Role, bool>> query, EFProviders.MemberShip context)
        {
            Role role = context.Role.Where(query).Where(MatchRoleApplication()).FirstOrDefault();
            if (role == null)
            {
                throw new ProviderException("The supplied role name could not be found.");
            }

            return role;
        }

        /// <summary>
        /// Matches the local application name.
        /// </summary>
        /// <returns>Status whether passed in user matches the application.</returns>
        private Expression<Func<Role, bool>> MatchRoleApplication()
        {
            return role => role.Application.Name == ApplicationName;
        }

        /// <summary>
        /// Matches the local application name.
        /// </summary>
        /// <returns>Status whether passed in user matches the application.</returns>
        private Expression<Func<User, bool>> MatchUserApplication()
        {
            return user => user.Application.Name == ApplicationName;
        }

        /// <summary>
        /// Writes exception to event log.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="action">The action.</param>
        /// <remarks>A helper function that writes exception detail to the event log. Exceptions
        /// are written to the event log as a security measure to avoid private database
        /// details from being returned to the browser. If a method does not return a status
        /// or boolean indicating the action succeeded or failed, a generic exception is also
        /// thrown by the caller.</remarks>
        private static void WriteToEventLog(Exception exception, string action)
        {
            EventLog log = new EventLog { Source = EVENTSOURCE, Log = EVENTLOG };

            StringBuilder message = new StringBuilder();
            message.Append(exceptionMessage + "\n\n");
            message.Append("Action: " + action + "\n\n");
            message.Append("Exception: " + exception);

            log.WriteEntry(message.ToString());
        }
        #endregion
    }
}
