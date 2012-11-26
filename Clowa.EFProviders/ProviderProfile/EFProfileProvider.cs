using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Profile;
using System.Linq.Expressions;
using System.Configuration;
using System.Configuration.Provider;
using System.Web.Hosting;
using System.Collections.Specialized;
using System.Security.Permissions;
using System.Globalization;
using System.IO;

namespace Clowa.EFProviders.ProviderProfile
{
    public class EFProfileProvider : ProfileProvider
    {

        #region members
        private string connectionString;
        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the name of the currently running application.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> that contains the application's shortened name, which does not contain a full path or extension, 
        /// for example, SimpleAppSettings.</returns>
        public override string ApplicationName { get; set; }

        #endregion

        #region public methods
        /// <summary>
        /// Initializes the provider.
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

            if (string.IsNullOrEmpty(name))
            {
                name = "EFProfileProvider";
            }

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Smart-Soft EF Profile Provider");
            }

            // Initialize base class
            base.Initialize(name, config);

            // Read connection string.
            ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[config["connectionStringName"]];

            if (connectionStringSettings == null || connectionStringSettings.ConnectionString.Trim() == string.Empty)
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = connectionStringSettings.ConnectionString;
            ApplicationName = Convert.ToString(ProviderUtils.GetConfigValue(config, "applicationName", HostingEnvironment.ApplicationVirtualPath));
        }

        /// <summary>
        /// Returns the collection of settings property values for the specified application instance and settings property group.
        /// </summary>
        /// <returns>A <see cref="T:System.Configuration.SettingsPropertyValueCollection" /> containing the values for the specified 
        /// settings property group.</returns>
        /// <param name="context">A <see cref="T:System.Configuration.SettingsContext" /> describing the current application use.</param>
        /// <param name="properties">A <see cref="T:System.Configuration.SettingsPropertyCollection" /> containing the settings property 
        /// group whose values are to be retrieved.</param>
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            SettingsPropertyValueCollection settings = new SettingsPropertyValueCollection();
            if (properties.Count >= 1)
            {
                string userName = (string)context["UserName"];
                foreach (SettingsProperty property in properties)
                {
                    if (property.SerializeAs == SettingsSerializeAs.ProviderSpecific)
                    {
                        // Determine serialization mode based on data type
                        if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
                        {
                            property.SerializeAs = SettingsSerializeAs.String;
                        }
                        else
                        {
                            property.SerializeAs = SettingsSerializeAs.Xml;
                        }
                    }

                    settings.Add(new SettingsPropertyValue(property));
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    ExtractPropertyValuesFromDatabase(userName, settings);
                }
            }

            return settings;
        }

        /// <summary>
        /// Sets the values of the specified group of property settings.
        /// </summary>
        /// <param name="context">A <see cref="T:System.Configuration.SettingsContext" /> describing the current application usage.</param>
        /// <param name="properties">A <see cref="T:System.Configuration.SettingsPropertyValueCollection" /> representing the group 
        /// of property settings to set.</param>
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection properties)
        {
            string username = (string)context["UserName"];

            if (string.IsNullOrEmpty(username) || properties.Count < 1)
            {
                return;
            }

            bool userIsAuthenticated = (bool)context["IsAuthenticated"];
            string propertiesNames = string.Empty;
            string propertiesValuesSerialized = string.Empty;
            byte[] propertiesValuesBinary = null;

            PrepareDataForSaving(ref propertiesNames, ref propertiesValuesSerialized, ref propertiesValuesBinary, properties, userIsAuthenticated);

            if (propertiesNames.Length != 0)
            {
                using (EFProviders.MemberShip dataContext = new EFProviders.MemberShip(connectionString))
                {
                    // Attempt to load user with associated profile
                    User user = dataContext.User.Include("Profile").Where(MatchUserApplication()).Where(u => u.Username == username).FirstOrDefault();

                    if (user == null)
                    {
                        throw new ArgumentException("user");
                    }

                    if (user.Profile == null)
                    {
                        // Create new profile
                        user.Profile = new Profile();
                    }

                    // Set profile values
                    user.Profile.PropertyNames = propertiesNames;
                    user.Profile.PropertyValuesString = propertiesValuesSerialized;
                    user.Profile.PropertyValuesBinary = propertiesValuesBinary;
                    user.Profile.LastUpdatedDate = DateTime.Now;
                    dataContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// When overridden in a derived class, deletes profile properties and information for the supplied list of profiles.
        /// </summary>
        /// <returns>The number of profiles deleted from the data source.</returns>
        /// <param name="profiles">A <see cref="T:System.Web.Profile.ProfileInfoCollection" />  of information about profiles that are to be deleted.</param>
        public override int DeleteProfiles(ProfileInfoCollection profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException("profiles");
            }

            if (profiles.Count < 1)
            {
                throw new ArgumentException("profiles");
            }

            string[] usernames = profiles.Cast<ProfileInfo>().Select(p => p.UserName).ToArray();
            return DeleteProfiles(usernames);
        }

        /// <summary>
        /// When overridden in a derived class, deletes profile properties and information for profiles that match the supplied list of user names.
        /// </summary>
        /// <returns>The number of profiles deleted from the data source.</returns>
        /// <param name="usernames">A string array of user names for profiles to be deleted.</param>
        public override int DeleteProfiles(string[] usernames)
        {
            int num = 0;

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<User> users = context.User.Include("Profile").Where(u => usernames.Contains(u.Username) && u.Profile != null);

                foreach (User user in users)
                {
                    context.DeleteObject(user.Profile);
                    num++;
                }

                context.SaveChanges();
            }

            return num;
        }

        /// <summary>
        /// When overridden in a derived class, deletes all user-profile data for profiles in which the last activity date occurred before the specified date.
        /// </summary>
        /// <returns>The number of profiles deleted from the data source.</returns>
        /// <param name="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption" /> values, specifying 
        /// whether anonymous, authenticated, or both types of profiles are deleted.</param>
        /// <param name="userInactiveSinceDate">A <see cref="T:System.DateTime" /> that identifies which user profiles are considered 
        /// inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate" />  value of a user profile occurs on or before 
        /// this date and time, the profile is considered inactive.</param>
        public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            int num = 0;

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            { 
                IQueryable<Profile> profiles = context.User.Where(MatchUserApplication())
                                                                     .Where(ApplyAuthenticationOption(authenticationOption))
                                                                     .Where(u => u.LastActivityDate <= userInactiveSinceDate.ToUniversalTime())
                                                                     .Select(u => u.Profile);

                foreach (Profile profile in profiles)
                {
                    context.DeleteObject(profile);
                    num++;
                }

                context.SaveChanges();
            }

            return num;
        }

        /// <summary>
        /// When overridden in a derived class, returns the number of profiles in which the last activity date occurred on or before the specified date.
        /// </summary>
        /// <returns>The number of profiles in which the last activity date occurred on or before the specified date.</returns>
        /// <param name="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption" /> values, specifying 
        /// whether anonymous, authenticated, or both types of profiles are returned.</param>
        /// <param name="userInactiveSinceDate">A <see cref="T:System.DateTime" /> that identifies which user profiles are considered inactive. 
        /// If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate" />  of a user profile occurs on or before this date and time, 
        /// the profile is considered inactive.</param>
        public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<Profile> profiles = context.User.Where(MatchUserApplication())
                                                                     .Where(ApplyAuthenticationOption(authenticationOption))
                                                                     .Where(u => u.LastActivityDate <= userInactiveSinceDate.ToUniversalTime())
                                                                     .Select(u => u.Profile);
                return profiles.Count();
            }
        }

        /// <summary>
        /// When overridden in a derived class, retrieves user profile data for all profiles in the data source.
        /// </summary>
        /// <returns>A <see cref="T:System.Web.Profile.ProfileInfoCollection" /> containing user-profile information for all profiles in the data source.</returns>
        /// <param name="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption" /> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
        /// <param name="pageIndex">The index of the page of results to return.</param>
        /// <param name="pageSize">The size of the page of results to return.</param>
        /// <param name="totalRecords">When this method returns, contains the total number of profiles.</param>
        public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex, int pageSize, out int totalRecords)
        {
            ProfileInfoCollection profileCollection = new ProfileInfoCollection();

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<ProfileInfo> profiles = context.User.Where(MatchUserApplication())
                                                               .Where(ApplyAuthenticationOption(authenticationOption))
                                                               .Skip(pageIndex * pageSize)
                                                               .Take(pageSize)
                                                               .Select(u => new ProfileInfo(u.Username, u.IsAnonymous, u.LastActivityDate, u.Profile.LastUpdatedDate, GetSize(u)));
                foreach (ProfileInfo profileInfo in profiles)
                {
                    profileCollection.Add(profileInfo);
                }

                totalRecords = context.User.Where(MatchUserApplication()).Where(ApplyAuthenticationOption(authenticationOption)).Count();
                return profileCollection;
            }
        }

        /// <summary>
        /// When overridden in a derived class, retrieves user-profile data from the data source for profiles in which the last activity date occurred on or before the specified date.
        /// </summary>
        /// <returns>A <see cref="T:System.Web.Profile.ProfileInfoCollection" /> containing user-profile information about the inactive profiles.</returns>
        /// <param name="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption" /> values, specifying whether anonymous, 
        /// authenticated, or both types of profiles are returned.</param>
        /// <param name="userInactiveSinceDate">A <see cref="T:System.DateTime" /> that identifies which user profiles are considered inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate" />  of a user profile occurs on or before this date and time, the profile is considered inactive.</param>
        /// <param name="pageIndex">The index of the page of results to return.</param>
        /// <param name="pageSize">The size of the page of results to return.</param>
        /// <param name="totalRecords">When this method returns, contains the total number of profiles.</param>
        public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            ProfileInfoCollection profileCollection = new ProfileInfoCollection();

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<ProfileInfo> profiles = context.User.Where(MatchUserApplication())
                                                               .Where(ApplyAuthenticationOption(authenticationOption))
                                                               .Where(u => u.LastActivityDate <= userInactiveSinceDate.ToUniversalTime())
                                                               .Skip(pageIndex * pageSize)
                                                               .Take(pageSize)
                                                               .Select(u => new ProfileInfo(u.Username, u.IsAnonymous, u.LastActivityDate, u.Profile.LastUpdatedDate, GetSize(u)));
                foreach (ProfileInfo profileInfo in profiles)
                {
                    profileCollection.Add(profileInfo);
                }

                totalRecords = context.User.Where(MatchUserApplication()).Where(u => u.LastActivityDate > userInactiveSinceDate.ToUniversalTime()).Count();
                return profileCollection;
            }
        }

        /// <summary>
        /// When overridden in a derived class, retrieves profile information for profiles in which the user name matches the specified user names.
        /// </summary>
        /// <returns>A <see cref="T:System.Web.Profile.ProfileInfoCollection" /> containing user-profile information for profiles where the user name matches the supplied <paramref name="usernameToMatch" /> parameter.</returns>
        /// <param name="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption" /> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
        /// <param name="usernameToMatch">The user name to search for.</param>
        /// <param name="pageIndex">The index of the page of results to return.</param>
        /// <param name="pageSize">The size of the page of results to return.</param>
        /// <param name="totalRecords">When this method returns, contains the total number of profiles.</param>
        public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            ProfileInfoCollection profileCollection = new ProfileInfoCollection();

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<ProfileInfo> profiles = context.User.Where(MatchUserApplication())
                                                               .Where(ApplyAuthenticationOption(authenticationOption))
                                                               .Where(u => u.Username.Contains(usernameToMatch))
                                                               .Skip(pageIndex * pageSize)
                                                               .Take(pageSize)
                                                               .Select(u => new ProfileInfo(u.Username, u.IsAnonymous, u.LastActivityDate, u.Profile.LastUpdatedDate, GetSize(u)));
                foreach (ProfileInfo profileInfo in profiles)
                {
                    profileCollection.Add(profileInfo);
                }

                totalRecords = context.User.Where(MatchUserApplication()).Where(ApplyAuthenticationOption(authenticationOption)).Where(u => u.Username.Contains(usernameToMatch)).Count();
                return profileCollection;
            }
        }

        /// <summary>
        /// When overridden in a derived class, retrieves profile information for profiles in which the last activity date occurred on or before the specified date and the user name matches the specified user name.
        /// </summary>
        /// <returns>A <see cref="T:System.Web.Profile.ProfileInfoCollection" /> containing user profile information for inactive profiles where the user name matches the supplied <paramref name="usernameToMatch" /> parameter.
        /// </returns>
        /// <param name="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption" /> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
        /// <param name="usernameToMatch">The user name to search for.</param>
        /// <param name="userInactiveSinceDate">A <see cref="T:System.DateTime" /> that identifies which user profiles are considered inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate" /> value of a user profile occurs on or before this date and time, the profile is considered inactive.</param>
        /// <param name="pageIndex">The index of the page of results to return.</param>
        /// <param name="pageSize">The size of the page of results to return.</param>
        /// <param name="totalRecords">When this method returns, contains the total number of profiles.</param>
        public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            ProfileInfoCollection profileCollection = new ProfileInfoCollection();

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                IQueryable<ProfileInfo> profiles = context.User.Where(MatchUserApplication())
                                                               .Where(ApplyAuthenticationOption(authenticationOption))
                                                               .Where(u => u.Username.Contains(usernameToMatch) && u.LastActivityDate <= userInactiveSinceDate.ToUniversalTime())
                                                               .Skip(pageIndex * pageSize)
                                                               .Take(pageSize)
                                                               .Select(u => new ProfileInfo(u.Username, u.IsAnonymous, u.LastActivityDate, u.Profile.LastUpdatedDate, GetSize(u)));
                foreach (ProfileInfo profileInfo in profiles)
                {
                    profileCollection.Add(profileInfo);
                }

                totalRecords = context.User.Where(MatchUserApplication())
                                           .Where(ApplyAuthenticationOption(authenticationOption))
                                           .Where(u => u.Username.Contains(usernameToMatch) && u.LastActivityDate <= userInactiveSinceDate.ToUniversalTime())
                                           .Count();
                return profileCollection;
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// Parses the data from database.
        /// </summary>
        /// <param name="propertiesNames">The properties names.</param>
        /// <param name="propertiesValuesSerialized">The properties values serialized.</param>
        /// <param name="propertiesValuesBinary">The properties values binary.</param>
        /// <param name="propertiesValues">The properties values.</param>
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        private static void ParseDataFromDatabase(string[] propertiesNames, string propertiesValuesSerialized, byte[] propertiesValuesBinary, SettingsPropertyValueCollection propertiesValues)
        {
            // Validate parameters
            if ((propertiesNames == null || propertiesValuesSerialized == null) || (propertiesValuesBinary == null || propertiesValues == null))
            {
                return;
            }

            try
            {
                for (int i = 0; i < (propertiesNames.Length / 4); i++)
                {
                    // Each property name definition consists of four parts (property name, serialization mode, data start index, data length)
                    string propertyName = propertiesNames[i * 4];
                    SettingsPropertyValue propertyValue = propertiesValues[propertyName];
                    if (propertyValue != null)
                    {
                        int startIndex = int.Parse(propertiesNames[(i * 4) + 2], CultureInfo.InvariantCulture);
                        int length = int.Parse(propertiesNames[(i * 4) + 3], CultureInfo.InvariantCulture);
                        if (length == -1 && !propertyValue.Property.PropertyType.IsValueType)
                        {
                            // No property value present
                            propertyValue.PropertyValue = null;
                            propertyValue.IsDirty = false;
                            propertyValue.Deserialized = true;
                        }

                        // Validate index and length
                        if (startIndex >= 0 &&
                            length > 0 &&
                            propertiesValuesSerialized.Length >= startIndex + length)
                        {
                            if (propertiesNames[(i * 4) + 1] == "S")
                            {
                                // String serialized data
                                propertyValue.SerializedValue = propertiesValuesSerialized.Substring(startIndex, length);
                            }
                            else if (propertiesNames[(i * 4) + 1] == "B")
                            {
                                // Binary data
                                byte[] serializedValue = new byte[length];
                                Buffer.BlockCopy(propertiesValuesBinary, startIndex, serializedValue, 0, length);
                                propertyValue.SerializedValue = serializedValue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ProviderException("Error while parsing values from database.", ex);
            }
        }

        /// <summary>
        /// Prepares the data for saving.
        /// </summary>
        /// <param name="propertiesNames">The properties names.</param>
        /// <param name="propertiesValuesSerialized">The properties values serialized.</param>
        /// <param name="propertiesValuesBinary">The properties values binary.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="userIsAuthenticated">if set to <c>true</c> [user is authenticated].</param>
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        private static void PrepareDataForSaving(ref string propertiesNames, ref string propertiesValuesSerialized, ref byte[] propertiesValuesBinary, SettingsPropertyValueCollection properties, bool userIsAuthenticated)
        {
            StringBuilder propertiesNamesBuilder = new StringBuilder();
            StringBuilder propertiesValuesSerializedBuilder = new StringBuilder();
            MemoryStream propertiesValuesBinarySerialized = new MemoryStream();
            try
            {
                try
                {
                    List<SettingsPropertyValue> relevantValues = properties.Cast<SettingsPropertyValue>()
                                                                           .Where(v => v.IsDirty && (userIsAuthenticated || (bool)v.Property.Attributes["AllowAnonymous"]))
                                                                           .ToList();
                    if (relevantValues.Count == 0)
                    {
                        return;
                    }

                    foreach (SettingsPropertyValue value in relevantValues)
                    {
                        int length;
                        int startPosition = 0;
                        string stringValue = null;

                        if (value.Deserialized && value.PropertyValue == null)
                        {
                            length = -1;
                        }
                        else
                        {
                            if (value.SerializedValue == null)
                            {
                                length = -1;
                            }
                            else
                            {
                                if (value.SerializedValue is string)
                                {
                                    stringValue = (string)value.SerializedValue;
                                    length = stringValue.Length;
                                    startPosition = propertiesValuesSerializedBuilder.Length;
                                }
                                else
                                {
                                    byte[] serializedBinary = (byte[])value.SerializedValue;
                                    startPosition = (int)propertiesValuesBinarySerialized.Position;
                                    propertiesValuesBinarySerialized.Write(serializedBinary, 0, serializedBinary.Length);
                                    propertiesValuesBinarySerialized.Position = startPosition + serializedBinary.Length;
                                    length = serializedBinary.Length;
                                }
                            }
                        }

                        propertiesNamesBuilder.Append(value.Name + ":");
                        propertiesNamesBuilder.Append(stringValue != null ? "S" : "B" + ":");
                        propertiesNamesBuilder.Append(startPosition.ToString(CultureInfo.InvariantCulture) + ":");
                        propertiesNamesBuilder.Append(length.ToString(CultureInfo.InvariantCulture) + ":");

                        if (stringValue != null)
                        {
                            propertiesValuesSerializedBuilder.Append(stringValue);
                        }
                    }

                    propertiesValuesBinary = propertiesValuesBinarySerialized.ToArray();
                }
                finally
                {
                    propertiesValuesBinarySerialized.Close();
                }
            }
            catch (Exception ex)
            {
                throw new ProviderException("Error while prepare data for saving.", ex);
            }

            propertiesNames = propertiesNamesBuilder.ToString();
            propertiesValuesSerialized = propertiesValuesSerializedBuilder.ToString();
        }

        /// <summary>
        /// Gets the property values from database.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="propertiesValues">The property values.</param>
        private void ExtractPropertyValuesFromDatabase(string userName, SettingsPropertyValueCollection propertiesValues)
        {
            string[] propertyNames = null;
            string propertiesValuesSerialized = null;
            byte[] propertiesValuesBinary = null;

            using (EFProviders.MemberShip context = new EFProviders.MemberShip(connectionString))
            {
                User user = context.User.Include("Profile").Where(MatchUserApplication()).Where(u => u.Username == userName && u.Profile != null).FirstOrDefault();
                if (user != null)
                {
                    propertyNames = user.Profile.PropertyNames.Split(new[] { ':' });
                    propertiesValuesSerialized = user.Profile.PropertyValuesString;
                    propertiesValuesBinary = user.Profile.PropertyValuesBinary;

                    // Update user
                    user.LastActivityDate = DateTime.Now;
                    context.SaveChanges();
                }
            }

            ParseDataFromDatabase(propertyNames, propertiesValuesSerialized, propertiesValuesBinary, propertiesValues);
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
        /// Applies the authentication option.
        /// </summary>
        /// <param name="authenticationOption">The authentication option.</param>
        /// <returns></returns>
        private Expression<Func<User, bool>> ApplyAuthenticationOption(ProfileAuthenticationOption authenticationOption)
        {
            switch (authenticationOption)
            {
                case ProfileAuthenticationOption.All:
                    return user => true;
                case ProfileAuthenticationOption.Anonymous:
                    return user => user.IsAnonymous;
                case ProfileAuthenticationOption.Authenticated:
                    return user => !user.IsAnonymous;
                default:
                    return user => true;
            }
        }

        /// <summary>
        /// Gets the data size.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The data size.</returns>
        private int GetSize(User user)
        {
            return user.Profile.PropertyNames.Length + user.Profile.PropertyValuesString.Length + user.Profile.PropertyValuesBinary.Length;
        }
        #endregion
    }
}
