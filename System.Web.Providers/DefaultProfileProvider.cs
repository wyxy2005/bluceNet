namespace System.Web.Providers
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Configuration;
    using System.Configuration.Provider;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web.Profile;
    using System.Web.Providers.Entities;
    using System.Web.Providers.Resources;
    using System.Xml.Serialization;

    public class DefaultProfileProvider : ProfileProvider
    {
        public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<ProfileEntity> queryable = QueryHelper.GetInactiveProfiles(context, this.ApplicationName, authenticationOption, userInactiveSinceDate);
                int num = 0;
                foreach (ProfileEntity entity in queryable)
                {
                    ProfileEntity entity2 = context.Profiles.Find(new object[] { entity.UserId });
                    context.Profiles.Remove(entity2);
                    num++;
                }
                context.SaveChanges();
                return num;
            }
        }

        public override int DeleteProfiles(string[] usernames)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "usernames";
            Exception exception = ValidationHelper.CheckArrayParameter(ref usernames, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            int num = 0;
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                foreach (string str in usernames)
                {
                    ProfileEntity entity = QueryHelper.GetProfile(context, this.ApplicationName, str);
                    if (entity != null)
                    {
                        num++;
                        context.Profiles.Remove(entity);
                    }
                }
                context.SaveChanges();
            }
            return num;
        }

        public override int DeleteProfiles(ProfileInfoCollection profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException("profiles");
            }
            if (profiles.Count < 1)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Parameter_collection_empty, new object[] { "profiles" }), "profiles");
            }
            int num = 0;
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                foreach (ProfileInfo info in profiles)
                {
                    ProfileEntity entity = QueryHelper.GetProfile(context, this.ApplicationName, info.UserName);
                    if (entity != null)
                    {
                        num++;
                        context.Profiles.Remove(entity);
                    }
                }
                context.SaveChanges();
            }
            return num;
        }

        private static object DeserializeFromString(SettingsProperty property, string value)
        {
            Type propertyType;
            TypeConverter converter;
            if (!string.IsNullOrEmpty(value))
            {
                propertyType = property.PropertyType;
                SettingsSerializeAs serializeAs = property.SerializeAs;
                if ((propertyType == typeof(string)) && (((value == null) || (value.Length < 1)) || (serializeAs == SettingsSerializeAs.String)))
                {
                    return value;
                }
                switch (serializeAs)
                {
                    case SettingsSerializeAs.String:
                        goto Label_00AB;

                    case SettingsSerializeAs.Xml:
                        goto Label_0082;

                    case SettingsSerializeAs.Binary:
                    {
                        byte[] buffer = Convert.FromBase64String(value);
                        MemoryStream stream = null;
                        try
                        {
                            stream = new MemoryStream(buffer);
                            return new NetDataContractSerializer().Deserialize(stream);
                        }
                        finally
                        {
                            if (stream != null)
                            {
                                stream.Close();
                            }
                        }
                        goto Label_0082;
                    }
                }
            }
            return null;
        Label_0082:
            using (StringReader reader = new StringReader(value))
            {
                XmlSerializer serializer = new XmlSerializer(propertyType);
                return serializer.Deserialize(reader);
            }
        Label_00AB:
            converter = TypeDescriptor.GetConverter(propertyType);
            if (((converter == null) || !converter.CanConvertTo(typeof(string))) || !converter.CanConvertFrom(typeof(string)))
            {
                throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Unable_to_convert_type_to_string, new object[] { propertyType.ToString() }));
            }
            return converter.ConvertFromInvariantString(value);
        }

        public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = false;
            int maxSize = 0x100;
            string paramName = "usernameToMatch";
            Exception exception = ValidationHelper.CheckParameter(ref usernameToMatch, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            ProfileInfoCollection infos = new ProfileInfoCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<QueryHelper.EFProfileInfo> source = QueryHelper.GetProfileInfos(context, this.ApplicationName, authenticationOption, userInactiveSinceDate, usernameToMatch);
                totalRecords = source.Count<QueryHelper.EFProfileInfo>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    source = source.Skip<QueryHelper.EFProfileInfo>(pageIndex * pageSize);
                }
                foreach (QueryHelper.EFProfileInfo info in source.Take<QueryHelper.EFProfileInfo>(pageSize))
                {
                    infos.Add(info.ToProfileInfo());
                }
            }
            return infos;
        }

        public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = false;
            int maxSize = 0x100;
            string paramName = "usernameToMatch";
            Exception exception = ValidationHelper.CheckParameter(ref usernameToMatch, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            ProfileInfoCollection infos = new ProfileInfoCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<QueryHelper.EFProfileInfo> source = QueryHelper.GetProfileInfos(context, this.ApplicationName, authenticationOption, DateTime.MaxValue, usernameToMatch);
                totalRecords = source.Count<QueryHelper.EFProfileInfo>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    source = source.Skip<QueryHelper.EFProfileInfo>(pageIndex * pageSize);
                }
                foreach (QueryHelper.EFProfileInfo info in source.Take<QueryHelper.EFProfileInfo>(pageSize))
                {
                    infos.Add(info.ToProfileInfo());
                }
            }
            return infos;
        }

        public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            ProfileInfoCollection infos = new ProfileInfoCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<QueryHelper.EFProfileInfo> source = QueryHelper.GetProfileInfos(context, this.ApplicationName, authenticationOption, userInactiveSinceDate, null);
                totalRecords = source.Count<QueryHelper.EFProfileInfo>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    source = source.Skip<QueryHelper.EFProfileInfo>(pageIndex * pageSize);
                }
                foreach (QueryHelper.EFProfileInfo info in source.Take<QueryHelper.EFProfileInfo>(pageSize))
                {
                    infos.Add(info.ToProfileInfo());
                }
            }
            return infos;
        }

        public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex, int pageSize, out int totalRecords)
        {
            ProfileInfoCollection infos = new ProfileInfoCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<QueryHelper.EFProfileInfo> source = QueryHelper.GetProfileInfos(context, this.ApplicationName, authenticationOption, DateTime.MaxValue, null);
                totalRecords = source.Count<QueryHelper.EFProfileInfo>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    source = source.Skip<QueryHelper.EFProfileInfo>(pageIndex * pageSize);
                }
                foreach (QueryHelper.EFProfileInfo info in source.Take<QueryHelper.EFProfileInfo>(pageSize))
                {
                    infos.Add(info.ToProfileInfo());
                }
            }
            return infos;
        }

        public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return QueryHelper.GetProfileInfos(context, this.ApplicationName, authenticationOption, userInactiveSinceDate, null).Count<QueryHelper.EFProfileInfo>();
            }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            SettingsPropertyValueCollection properties = new SettingsPropertyValueCollection();
            if (collection.Count >= 1)
            {
                string str = (string) context["UserName"];
                foreach (SettingsProperty property in collection)
                {
                    if (property.SerializeAs == SettingsSerializeAs.ProviderSpecific)
                    {
                        if (property.PropertyType.IsPrimitive || (property.PropertyType == typeof(string)))
                        {
                            property.SerializeAs = SettingsSerializeAs.String;
                        }
                        else
                        {
                            property.SerializeAs = SettingsSerializeAs.Xml;
                        }
                    }
                    properties.Add(new SettingsPropertyValue(property));
                }
                if (string.IsNullOrEmpty(str))
                {
                    return properties;
                }
                using (MembershipContext context2 = ModelHelper.CreateMembershipContext(this.ConnectionString))
                {
                    ProfileEntity entity = QueryHelper.GetProfile(context2, this.ApplicationName, str);
                    if (entity != null)
                    {
                        ParseDataFromDB(entity.PropertyNames.Split(new char[] { ':' }), entity.PropertyValueStrings, properties);
                    }
                }
            }
            return properties;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (string.IsNullOrEmpty(name))
            {
                name = "DefaultProfileProvider";
            }
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", string.Format(CultureInfo.CurrentCulture, ProviderResources.ProfileProvider_description, new object[0]));
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
            this.ConnectionString = ModelHelper.GetConnectionString(config["connectionStringName"]);
            config.Remove("connectionStringName");
            if (config.Count > 0)
            {
                string key = config.GetKey(0);
                if (!string.IsNullOrEmpty(key))
                {
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_unrecognized_attribute, new object[] { key }));
                }
            }
        }

        private static void ParseDataFromDB(string[] names, string values, SettingsPropertyValueCollection properties)
        {
            if (((names != null) && (values != null)) && (properties != null))
            {
                try
                {
                    for (int i = 0; i < (names.Length / 3); i++)
                    {
                        int index = i * 3;
                        string str = names[index];
                        SettingsPropertyValue value2 = properties[str];
                        if (value2 != null)
                        {
                            int startIndex = int.Parse(names[index + 1], CultureInfo.InvariantCulture);
                            int length = int.Parse(names[index + 2], CultureInfo.InvariantCulture);
                            if ((length == -1) && !value2.Property.PropertyType.IsValueType)
                            {
                                value2.PropertyValue = null;
                                value2.IsDirty = false;
                                value2.Deserialized = true;
                            }
                            else if (((startIndex >= 0) && (length > 0)) && (values.Length >= (startIndex + length)))
                            {
                                value2.PropertyValue = DeserializeFromString(value2.Property, values.Substring(startIndex, length));
                                value2.IsDirty = false;
                                value2.Deserialized = true;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static void PrepareDataForSaving(ref string allNames, ref string allValues, SettingsPropertyValueCollection properties, bool userIsAuthenticated)
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            bool flag = false;
            foreach (SettingsPropertyValue value2 in properties)
            {
                if (value2.IsDirty && (userIsAuthenticated || ((bool) value2.Property.Attributes["AllowAnonymous"])))
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                foreach (SettingsPropertyValue value3 in properties)
                {
                    if ((!userIsAuthenticated && !((bool) value3.Property.Attributes["AllowAnonymous"])) || (!value3.IsDirty && value3.UsingDefaultValue))
                    {
                        continue;
                    }
                    int length = 0;
                    int num2 = 0;
                    string str = null;
                    if (value3.Deserialized && (value3.PropertyValue == null))
                    {
                        length = -1;
                    }
                    else
                    {
                        str = SerializeToString(value3);
                        length = str.Length;
                        num2 = builder2.Length;
                    }
                    builder.Append(value3.Name + ":" + num2.ToString(CultureInfo.InvariantCulture) + ":" + length.ToString(CultureInfo.InvariantCulture) + ":");
                    if (str != null)
                    {
                        builder2.Append(str);
                    }
                }
                allNames = builder.ToString();
                allValues = builder2.ToString();
            }
        }

        private static string SerializeToString(SettingsPropertyValue value)
        {
            if (value.PropertyValue == null)
            {
                return null;
            }
            SettingsSerializeAs serializeAs = value.Property.SerializeAs;
            Type propertyType = value.Property.PropertyType;
            if (serializeAs == SettingsSerializeAs.ProviderSpecific)
            {
                if ((propertyType == typeof(string)) || propertyType.IsPrimitive)
                {
                    serializeAs = SettingsSerializeAs.String;
                }
                else
                {
                    serializeAs = SettingsSerializeAs.Xml;
                }
            }
            switch (serializeAs)
            {
                case SettingsSerializeAs.String:
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(propertyType);
                    if (((converter == null) || !converter.CanConvertTo(typeof(string))) || !converter.CanConvertFrom(typeof(string)))
                    {
                        throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Unable_to_convert_type_to_string, new object[] { propertyType.ToString() }));
                    }
                    return converter.ConvertToInvariantString(value.PropertyValue);
                }
                case SettingsSerializeAs.Xml:
                {
                    using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        new XmlSerializer(propertyType).Serialize((TextWriter) writer, value.PropertyValue);
                        return writer.ToString();
                    }
                }
                case SettingsSerializeAs.Binary:
                {
                    MemoryStream stream = new MemoryStream();
                    try
                    {
                        new NetDataContractSerializer().Serialize(stream, value.PropertyValue);
                        return Convert.ToBase64String(stream.ToArray());
                    }
                    finally
                    {
                        stream.Close();
                    }
                    goto Label_012F;
                }
            }
        Label_012F:
            return null;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            string str = (string) context["UserName"];
            bool userIsAuthenticated = (bool) context["IsAuthenticated"];
            if (!string.IsNullOrEmpty(str) && (collection.Count > 0))
            {
                string allNames = string.Empty;
                string allValues = string.Empty;
                PrepareDataForSaving(ref allNames, ref allValues, collection, userIsAuthenticated);
                if (allNames.Length != 0)
                {
                    using (MembershipContext context2 = ModelHelper.CreateMembershipContext(this.ConnectionString))
                    {
                        User user2;
                        ProfileEntity profile;
                        QueryHelper.ProfileAndUser user = QueryHelper.GetProfileAndUser(context2, this.ApplicationName, str);
                        if (((user == null) || (user.User == null)) || (user.Profile == null))
                        {
                            bool createIfNotExist = true;
                            Application application = QueryHelper.GetApplication(context2, this.ApplicationName, createIfNotExist);
                            user2 = QueryHelper.GetUser(context2, str, application);
                            if (user2 == null)
                            {
                                user2 = ModelHelper.CreateUser(context2, Guid.NewGuid(), str, application.ApplicationId, !userIsAuthenticated);
                            }
                            profile = new ProfileEntity {
                                UserId = user2.UserId
                            };
                            context2.Profiles.Add(profile);
                        }
                        else
                        {
                            profile = user.Profile;
                            user2 = user.User;
                        }
                        user2.LastActivityDate = DateTime.UtcNow;
                        profile.LastUpdatedDate = DateTime.UtcNow;
                        profile.PropertyNames = allNames;
                        profile.PropertyValueStrings = allValues;
                        profile.PropertyValueBinary = new byte[0];
                        context2.SaveChanges();
                    }
                }
            }
        }

        public override string ApplicationName { get; set; }

        private ConnectionStringSettings ConnectionString { get; set; }
    }
}

