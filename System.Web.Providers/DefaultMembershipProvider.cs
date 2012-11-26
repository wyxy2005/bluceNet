namespace System.Web.Providers
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Configuration.Provider;
    using System.Data.Common;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Configuration;
    using System.Web.Providers.Entities;
    using System.Web.Providers.Resources;
    using System.Web.Security;

    public class DefaultMembershipProvider : MembershipProvider
    {
        private string _configHashAlgorithmType;
        private string _HashAlgorithm;
        private bool? _hashAlgorithmFromConfig;
        private MembershipPasswordCompatibilityMode _legacyPasswordCompatibilityMode = MembershipPasswordCompatibilityMode.Framework40;
        private Regex _PasswordStrengthRegEx;
        internal const int MaxPasswordSize = 0x80;
        internal const int MaxSimpleStringSize = 0x100;
        internal static DateTime NullDate = new DateTime(0x6da, 1, 1);
        internal const int SALT_SIZE = 0x10;

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
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
            bool flag7 = false;
            int num4 = 0x80;
            string str3 = "oldPassword";
            exception = ValidationHelper.CheckParameter(ref oldPassword, flag5, flag6, flag7, num4, str3);
            if (exception != null)
            {
                throw exception;
            }
            bool flag8 = true;
            bool flag9 = true;
            bool flag10 = false;
            int num5 = 0x80;
            string str4 = "newPassword";
            exception = ValidationHelper.CheckParameter(ref newPassword, flag8, flag9, flag10, num5, str4);
            if (exception != null)
            {
                throw exception;
            }
            if (newPassword.Length < this.MinRequiredPasswordLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Password_too_short, new object[] { "newPassword", this.MinRequiredPasswordLength.ToString(CultureInfo.InvariantCulture) }), "newPassword");
            }
            if (this.MinRequiredNonAlphanumericCharacters > 0)
            {
                int num = 0;
                for (int i = 0; i < newPassword.Length; i++)
                {
                    if (!char.IsLetterOrDigit(newPassword[i]))
                    {
                        num++;
                    }
                }
                if (num < this.MinRequiredNonAlphanumericCharacters)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Password_need_more_non_alpha_numeric_chars, new object[] { "newPassword", this.MinRequiredNonAlphanumericCharacters.ToString(CultureInfo.InvariantCulture) }), "newPassword");
                }
            }
            if ((this._PasswordStrengthRegEx != null) && !this._PasswordStrengthRegEx.IsMatch(newPassword))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Password_does_not_match_regular_expression, new object[] { "newPassword" }), "newPassword");
            }
            ValidatePasswordEventArgs e = new ValidatePasswordEventArgs(username, newPassword, true);
            this.OnValidatingPassword(e);
            if (e.Cancel)
            {
                if (e.FailureInformation != null)
                {
                    throw e.FailureInformation;
                }
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Membership_Custom_Password_Validation_Failure, new object[0]), "newPassword");
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                MembershipEntity membership = QueryHelper.GetMembership(context, this.ApplicationName, username);
                if (membership == null)
                {
                    return false;
                }
                bool updateLastActivityDate = false;
                bool failIfNotApproved = false;
                if (!this.CheckPassword(membership, null, oldPassword, updateLastActivityDate, failIfNotApproved))
                {
                    return false;
                }
                string str = this.EncodePassword(newPassword, membership.PasswordFormat, membership.PasswordSalt);
                membership.Password = str;
                membership.LastPasswordChangedDate = DateTime.UtcNow;
                context.SaveChanges();
                return true;
            }
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
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
            bool flag7 = false;
            int num2 = 0x80;
            string str3 = "password";
            exception = ValidationHelper.CheckParameter(ref password, flag5, flag6, flag7, num2, str3);
            if (exception != null)
            {
                throw exception;
            }
            bool requiresQuestionAndAnswer = this.RequiresQuestionAndAnswer;
            bool flag9 = this.RequiresQuestionAndAnswer;
            bool flag10 = false;
            int num3 = 0x100;
            string str4 = "newPasswordQuestion";
            exception = ValidationHelper.CheckParameter(ref newPasswordQuestion, requiresQuestionAndAnswer, flag9, flag10, num3, str4);
            if (exception != null)
            {
                throw exception;
            }
            bool flag11 = this.RequiresQuestionAndAnswer;
            bool flag12 = this.RequiresQuestionAndAnswer;
            bool flag13 = false;
            int num4 = 0x80;
            string str5 = "newPasswordAnswer";
            exception = ValidationHelper.CheckParameter(ref newPasswordAnswer, flag11, flag12, flag13, num4, str5);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                string str;
                MembershipEntity membership = QueryHelper.GetMembership(context, this.ApplicationName, username);
                if (membership == null)
                {
                    return false;
                }
                bool updateLastActivityDate = false;
                bool failIfNotApproved = false;
                if (!this.CheckPassword(membership, null, password, updateLastActivityDate, failIfNotApproved))
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(newPasswordAnswer))
                {
                    str = this.EncodePassword(newPasswordAnswer, membership.PasswordFormat, membership.PasswordSalt);
                }
                else
                {
                    str = newPasswordAnswer;
                }
                bool flag16 = this.RequiresQuestionAndAnswer;
                bool flag17 = this.RequiresQuestionAndAnswer;
                bool flag18 = false;
                int num5 = 0x80;
                string str6 = "newPasswordAnswer";
                exception = ValidationHelper.CheckParameter(ref str, flag16, flag17, flag18, num5, str6);
                if (exception != null)
                {
                    throw exception;
                }
                membership.PasswordQuestion = newPasswordQuestion;
                membership.PasswordAnswer = str;
                context.SaveChanges();
                return true;
            }
        }

        private bool CheckPassword(MembershipEntity membership, User user, string password, bool updateLastActivityDate, bool failIfNotApproved)
        {
            DateTime utcNow = DateTime.UtcNow;
            if ((membership == null) || membership.IsLockedOut)
            {
                return false;
            }
            if (!membership.IsApproved && failIfNotApproved)
            {
                return false;
            }
            bool flag = string.Compare(this.EncodePassword(password, membership.PasswordFormat, membership.PasswordSalt), membership.Password, StringComparison.Ordinal) == 0;
            if (flag)
            {
                if ((membership.FailedPasswordAttemptCount > 0) || (membership.FailedPasswordAnswerAttemptCount > 0))
                {
                    membership.FailedPasswordAnswerAttemptCount = 0;
                    membership.FailedPasswordAnswerAttemptWindowsStart = NullDate;
                    membership.FailedPasswordAttemptCount = 0;
                    membership.FailedPasswordAttemptWindowStart = NullDate;
                    membership.LastLockoutDate = NullDate;
                }
            }
            else
            {
                if (utcNow > membership.FailedPasswordAttemptWindowStart.AddMinutes((double) this.PasswordAttemptWindow))
                {
                    membership.FailedPasswordAttemptCount = 1;
                }
                else
                {
                    membership.FailedPasswordAttemptCount++;
                }
                membership.FailedPasswordAttemptWindowStart = utcNow;
                if (membership.FailedPasswordAttemptCount >= this.MaxInvalidPasswordAttempts)
                {
                    membership.IsLockedOut = true;
                    membership.LastLockoutDate = utcNow;
                }
            }
            if (updateLastActivityDate)
            {
                membership.LastLoginDate = utcNow;
                if (user != null)
                {
                    user.LastActivityDate = utcNow;
                }
            }
            return flag;
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            string str3;
            DateTime time;
            string salt = GenerateSalt();
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = false;
            int maxSize = 0x80;
            if (!ValidationHelper.ValidateParameter(ref password, checkForNull, checkIfEmpty, checkForCommas, maxSize))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }
            string str2 = this.EncodePassword(password, (int) this.PasswordFormat, salt);
            if (passwordAnswer != null)
            {
                passwordAnswer = passwordAnswer.Trim();
            }
            if (!string.IsNullOrEmpty(passwordAnswer))
            {
                str3 = this.EncodePassword(passwordAnswer, (int) this.PasswordFormat, salt);
            }
            else
            {
                str3 = passwordAnswer;
            }
            bool requiresQuestionAndAnswer = this.RequiresQuestionAndAnswer;
            bool flag5 = true;
            bool flag6 = false;
            int num5 = 0x80;
            if (!ValidationHelper.ValidateParameter(ref str3, requiresQuestionAndAnswer, flag5, flag6, num5))
            {
                status = MembershipCreateStatus.InvalidAnswer;
                return null;
            }
            bool flag7 = this.RequiresQuestionAndAnswer;
            bool flag8 = true;
            bool flag9 = false;
            int num6 = 0x100;
            if (!ValidationHelper.ValidateParameter(ref passwordQuestion, flag7, flag8, flag9, num6))
            {
                status = MembershipCreateStatus.InvalidQuestion;
                return null;
            }
            bool flag10 = true;
            bool flag11 = true;
            bool flag12 = true;
            int num7 = 0x100;
            if (!ValidationHelper.ValidateParameter(ref username, flag10, flag11, flag12, num7))
            {
                status = MembershipCreateStatus.InvalidUserName;
                return null;
            }
            bool requiresUniqueEmail = this.RequiresUniqueEmail;
            bool flag14 = this.RequiresUniqueEmail;
            bool flag15 = false;
            int num8 = 0x100;
            if (!ValidationHelper.ValidateParameter(ref email, requiresUniqueEmail, flag14, flag15, num8))
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            }
            if ((providerUserKey != null) && !(providerUserKey is Guid))
            {
                status = MembershipCreateStatus.InvalidProviderUserKey;
                return null;
            }
            if ((password == null) || (password.Length < this.MinRequiredPasswordLength))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }
            if (this.MinRequiredNonAlphanumericCharacters > 0)
            {
                int num = 0;
                for (int i = 0; i < password.Length; i++)
                {
                    if (!char.IsLetterOrDigit(password[i]))
                    {
                        num++;
                    }
                }
                if (num < this.MinRequiredNonAlphanumericCharacters)
                {
                    status = MembershipCreateStatus.InvalidPassword;
                    return null;
                }
            }
            if ((this._PasswordStrengthRegEx != null) && !this._PasswordStrengthRegEx.IsMatch(password))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }
            ValidatePasswordEventArgs e = new ValidatePasswordEventArgs(username, password, true);
            this.OnValidatingPassword(e);
            if (e.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }
            int num3 = this.Membership_CreateUser(this.ApplicationName, username, str2, salt, email, passwordQuestion, str3, isApproved, out time, this.RequiresUniqueEmail, (int) this.PasswordFormat, ref providerUserKey);
            if ((num3 < 0) || (num3 > 11))
            {
                num3 = 11;
            }
            status = (MembershipCreateStatus) num3;
            if (status != MembershipCreateStatus.Success)
            {
                return null;
            }
            return new MembershipUser(this.ProviderName, username, providerUserKey, email, passwordQuestion, null, isApproved, false, time, time, time, time, NullDate);
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "username";
            Exception exception = ValidationHelper.CheckParameter(ref username, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                MembershipEntity entity = QueryHelper.GetMembership(context, this.ApplicationName, username);
                if (entity == null)
                {
                    return false;
                }
                context.Memberships.Remove(entity);
                if (deleteAllRelatedData)
                {
                    foreach (UsersInRole role in QueryHelper.GetUserRolesForUser(context, this.ApplicationName, username))
                    {
                        context.UsersInRoles.Remove(role);
                    }
                    ProfileEntity entity2 = QueryHelper.GetProfile(context, this.ApplicationName, username);
                    if (entity2 != null)
                    {
                        context.Profiles.Remove(entity2);
                    }
                    User user = QueryHelper.GetUser(context, entity.UserId, this.ApplicationName);
                    if (user != null)
                    {
                        context.Users.Remove(user);
                    }
                }
                context.SaveChanges();
                return true;
            }
        }

        private string EncodePassword(string pass, int passwordFormat, string salt)
        {
            if (passwordFormat == 0)
            {
                return pass;
            }
            byte[] bytes = Encoding.Unicode.GetBytes(pass);
            byte[] src = Convert.FromBase64String(salt);
            byte[] inArray = null;
            if (passwordFormat == 1)
            {
                HashAlgorithm hashAlgorithm = this.GetHashAlgorithm();
                KeyedHashAlgorithm algorithm2 = hashAlgorithm as KeyedHashAlgorithm;
                if (algorithm2 != null)
                {
                    if (algorithm2.Key.Length == src.Length)
                    {
                        algorithm2.Key = src;
                    }
                    else if (algorithm2.Key.Length < src.Length)
                    {
                        byte[] dst = new byte[algorithm2.Key.Length];
                        Buffer.BlockCopy(src, 0, dst, 0, dst.Length);
                        algorithm2.Key = dst;
                    }
                    else
                    {
                        int num2;
                        byte[] buffer5 = new byte[algorithm2.Key.Length];
                        for (int i = 0; i < buffer5.Length; i += num2)
                        {
                            num2 = Math.Min(src.Length, buffer5.Length - i);
                            Buffer.BlockCopy(src, 0, buffer5, i, num2);
                        }
                        algorithm2.Key = buffer5;
                    }
                    inArray = algorithm2.ComputeHash(bytes);
                }
                else
                {
                    byte[] buffer6 = new byte[src.Length + bytes.Length];
                    Buffer.BlockCopy(src, 0, buffer6, 0, src.Length);
                    Buffer.BlockCopy(bytes, 0, buffer6, src.Length, bytes.Length);
                    inArray = hashAlgorithm.ComputeHash(buffer6);
                }
            }
            else
            {
                byte[] buffer7 = new byte[src.Length + bytes.Length];
                Buffer.BlockCopy(src, 0, buffer7, 0, src.Length);
                Buffer.BlockCopy(bytes, 0, buffer7, src.Length, bytes.Length);
                inArray = this.EncryptPassword(buffer7, this.LegacyPasswordCompatibilityMode);
            }
            return Convert.ToBase64String(inArray);
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            bool requiresUniqueEmail = this.RequiresUniqueEmail;
            bool checkIfEmpty = this.RequiresUniqueEmail;
            bool checkForCommas = false;
            int maxSize = 0x100;
            string paramName = "emailToMatch";
            Exception exception = ValidationHelper.CheckParameter(ref emailToMatch, requiresUniqueEmail, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            MembershipUserCollection users = new MembershipUserCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<DbDataRecord> source = QueryHelper.GetAllMembershipUsersLikeEmail(context, this.ApplicationName, emailToMatch);
                totalRecords = source.Count<DbDataRecord>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    source = source.Skip<DbDataRecord>((pageIndex * pageSize)).Take<DbDataRecord>(pageSize);
                }
                foreach (DbDataRecord record in source)
                {
                    users.Add(QueryHelper.Convert<QueryHelper.EFMembershipUser>(record).Convert(this.ProviderName));
                }
            }
            return users;
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "usernameToMatch";
            Exception exception = ValidationHelper.CheckParameter(ref usernameToMatch, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            MembershipUserCollection users = new MembershipUserCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<DbDataRecord> source = QueryHelper.GetAllMembershipUsersLikeUserName(context, this.ApplicationName, usernameToMatch);
                totalRecords = source.Count<DbDataRecord>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    source = source.Skip<DbDataRecord>((pageIndex * pageSize)).Take<DbDataRecord>(pageSize);
                }
                foreach (DbDataRecord record in source)
                {
                    users.Add(QueryHelper.Convert<QueryHelper.EFMembershipUser>(record).Convert(this.ProviderName));
                }
            }
            return users;
        }

        private static string GenerateSalt()
        {
            using (RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[0x10];
                provider.GetBytes(data);
                return Convert.ToBase64String(data);
            }
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection users = new MembershipUserCollection();
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                IQueryable<QueryHelper.EFMembershipUser> allMembershipUsers = QueryHelper.GetAllMembershipUsers(context, this.ApplicationName);
                totalRecords = allMembershipUsers.Count<QueryHelper.EFMembershipUser>();
                if ((pageIndex != -1) && (pageSize != -1))
                {
                    allMembershipUsers = allMembershipUsers.Skip<QueryHelper.EFMembershipUser>((pageIndex * pageSize)).Take<QueryHelper.EFMembershipUser>(pageSize);
                }
                foreach (QueryHelper.EFMembershipUser user in allMembershipUsers)
                {
                    users.Add(user.Convert(this.ProviderName));
                }
            }
            return users;
        }

        private string GetEncodedPasswordAnswer(MembershipEntity member, string passwordAnswer)
        {
            if (passwordAnswer != null)
            {
                passwordAnswer = passwordAnswer.Trim();
            }
            if (string.IsNullOrEmpty(passwordAnswer))
            {
                return passwordAnswer;
            }
            return this.EncodePassword(passwordAnswer, member.PasswordFormat, member.PasswordSalt);
        }

        private static string GetExceptionText(int status)
        {
            switch (status)
            {
                case 0:
                    return string.Empty;

                case 1:
                    return ProviderResources.Membership_UserNotFound;

                case 2:
                    return ProviderResources.Membership_WrongPassword;

                case 3:
                    return ProviderResources.Membership_WrongAnswer;

                case 4:
                    return ProviderResources.Membership_InvalidPassword;

                case 5:
                    return ProviderResources.Membership_InvalidQuestion;

                case 6:
                    return ProviderResources.Membership_InvalidAnswer;

                case 7:
                    return ProviderResources.Membership_InvalidEmail;

                case 0x63:
                    return ProviderResources.Membership_AccountLockOut;
            }
            return ProviderResources.Provider_Error;
        }

        internal HashAlgorithm GetHashAlgorithm()
        {
            if (this._HashAlgorithm != null)
            {
                return HashAlgorithm.Create(this._HashAlgorithm);
            }
            string hashAlgorithmType = Membership.HashAlgorithmType;
            if (((this.LegacyPasswordCompatibilityMode == MembershipPasswordCompatibilityMode.Framework20) && !this.IsHashAlgorithmFromMembershipConfig) && (hashAlgorithmType != "MD5"))
            {
                hashAlgorithmType = "SHA1";
            }
            HashAlgorithm algorithm = HashAlgorithm.Create(hashAlgorithmType);
            if (algorithm == null)
            {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Invalid_hash_algorithm, new object[] { this._HashAlgorithm }));
            }
            this._HashAlgorithm = hashAlgorithmType;
            return algorithm;
        }

        private static string GetHashAlgorithmFromConfig()
        {
            try
            {
                MembershipSection section = ConfigurationManager.GetSection("system.web/membership") as MembershipSection;
                if (section != null)
                {
                    return section.HashAlgorithmType;
                }
            }
            catch (SecurityException)
            {
            }
            return null;
        }

        public override int GetNumberOfUsersOnline()
        {
            DateTime dateactive = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes((double) Membership.UserIsOnlineTimeWindow));
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return QueryHelper.GetNumberOfOnlineUsers(context, this.ApplicationName, dateactive);
            }
        }

        public override string GetPassword(string username, string answer)
        {
            if (!this.EnablePasswordRetrieval)
            {
                throw new NotSupportedException(ProviderResources.Membership_PasswordRetrieval_not_supported);
            }
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "username";
            Exception exception = ValidationHelper.CheckParameter(ref username, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            int status = 0;
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                MembershipEntity member = QueryHelper.GetMembership(context, this.ApplicationName, username);
                if (member == null)
                {
                    status = 1;
                }
                else if (member.IsLockedOut)
                {
                    status = 0x63;
                }
                else
                {
                    string encodedPasswordAnswer;
                    DateTime utcNow = DateTime.UtcNow;
                    if (answer != null)
                    {
                        encodedPasswordAnswer = this.GetEncodedPasswordAnswer(member, answer);
                    }
                    else
                    {
                        encodedPasswordAnswer = answer;
                    }
                    bool requiresQuestionAndAnswer = this.RequiresQuestionAndAnswer;
                    bool flag5 = this.RequiresQuestionAndAnswer;
                    bool flag6 = false;
                    int num3 = 0x80;
                    string str4 = "passwordAnswer";
                    exception = ValidationHelper.CheckParameter(ref encodedPasswordAnswer, requiresQuestionAndAnswer, flag5, flag6, num3, str4);
                    if (exception != null)
                    {
                        throw exception;
                    }
                    if (this.RequiresQuestionAndAnswer && (member.PasswordAnswer != null))
                    {
                        if (string.Compare(member.PasswordAnswer, encodedPasswordAnswer, StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            member.FailedPasswordAnswerAttemptWindowsStart = utcNow;
                            if (utcNow > member.FailedPasswordAnswerAttemptWindowsStart.AddMinutes((double) this.PasswordAttemptWindow))
                            {
                                member.FailedPasswordAnswerAttemptCount = 1;
                            }
                            else
                            {
                                member.FailedPasswordAnswerAttemptCount++;
                            }
                            if (member.FailedPasswordAnswerAttemptCount >= this.MaxInvalidPasswordAttempts)
                            {
                                member.IsLockedOut = true;
                                member.LastLockoutDate = utcNow;
                            }
                            status = 3;
                        }
                        else if (member.FailedPasswordAnswerAttemptCount > 0)
                        {
                            member.FailedPasswordAnswerAttemptCount = 0;
                            member.FailedPasswordAnswerAttemptWindowsStart = NullDate;
                        }
                        context.SaveChanges();
                    }
                }
                if ((status == 0) && (member.Password != null))
                {
                    return this.UnEncodePassword(member.Password, member.PasswordFormat);
                }
            }
            ValidateStatus(status);
            return null;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (providerUserKey == null)
            {
                throw new ArgumentNullException("providerUserKey");
            }
            if (!(providerUserKey is Guid))
            {
                throw new ArgumentException(ProviderResources.Membership_InvalidProviderUserKey, "providerUserKey");
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return QueryHelper.GetMembershipUser(context, (Guid) providerUserKey, this.ApplicationName, userIsOnline, this.ProviderName);
            }
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
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
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return QueryHelper.GetMembershipUser(context, username, this.ApplicationName, userIsOnline, this.ProviderName);
            }
        }

        public override string GetUserNameByEmail(string email)
        {
            bool requiresUniqueEmail = this.RequiresUniqueEmail;
            bool checkIfEmpty = this.RequiresUniqueEmail;
            bool checkForCommas = false;
            int maxSize = 0x100;
            string paramName = "email";
            Exception exception = ValidationHelper.CheckParameter(ref email, requiresUniqueEmail, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                return QueryHelper.GetUserNameFromEmail(context, email, this.ApplicationName);
            }
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (string.IsNullOrEmpty(name))
            {
                name = "DefaultMembershipProvider";
            }
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", ProviderResources.MembershipProvider_description);
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
            this.ConnectionString = ModelHelper.GetConnectionString(config["connectionStringName"]);
            config.Remove("connectionStringName");
            if (config["enablePasswordReset"] != null)
            {
                this.EnablePasswordResetInternal = Convert.ToBoolean(config["enablePasswordReset"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.EnablePasswordResetInternal = true;
            }
            if (config["enablePasswordRetrieval"] != null)
            {
                this.EnablePasswordRetrievalInternal = Convert.ToBoolean(config["enablePasswordRetrieval"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.EnablePasswordRetrievalInternal = false;
            }
            if (config["maxInvalidPasswordAttempts"] != null)
            {
                this.MaxInvalidPasswordAttemptsInternal = Convert.ToInt32(config["maxInvalidPasswordAttempts"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.MaxInvalidPasswordAttemptsInternal = 5;
            }
            if (config["minRequiredNonalphanumericCharacters"] != null)
            {
                this.MinRequiredNonAlphanumericCharactersInternal = Convert.ToInt32(config["minRequiredNonalphanumericCharacters"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.MinRequiredNonAlphanumericCharactersInternal = 1;
            }
            if (config["minRequiredPasswordLength"] != null)
            {
                this.MinRequiredPasswordLengthInternal = Convert.ToInt32(config["minRequiredPasswordLength"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.MinRequiredPasswordLengthInternal = 7;
            }
            if (config["passwordAttemptWindow"] != null)
            {
                this.PasswordAttemptWindowInternal = Convert.ToInt32(config["passwordAttemptWindow"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.PasswordAttemptWindowInternal = 10;
            }
            if (config["passwordFormat"] != null)
            {
                this.PasswordFormatInternal = (MembershipPasswordFormat) Enum.Parse(typeof(MembershipPasswordFormat), config["passwordFormat"]);
            }
            else
            {
                this.PasswordFormatInternal = MembershipPasswordFormat.Hashed;
            }
            if (config["passwordStrengthRegularExpression"] != null)
            {
                this.PasswordStrengthRegularExpressionInternal = config["passwordStrengthRegularExpression"];
                try
                {
                    this._PasswordStrengthRegEx = new Regex(this.PasswordStrengthRegularExpressionInternal);
                    goto Label_024F;
                }
                catch (ArgumentException exception)
                {
                    throw new ProviderException(exception.Message, exception);
                }
            }
            this.PasswordStrengthRegularExpressionInternal = string.Empty;
        Label_024F:
            if (config["requiresQuestionAndAnswer"] != null)
            {
                this.RequiresQuestionAndAnswerInternal = Convert.ToBoolean(config["requiresQuestionAndAnswer"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.RequiresQuestionAndAnswerInternal = true;
            }
            if (config["requiresUniqueEmail"] != null)
            {
                this.RequiresUniqueEmailInternal = Convert.ToBoolean(config["requiresUniqueEmail"], CultureInfo.InvariantCulture);
            }
            else
            {
                this.RequiresUniqueEmailInternal = true;
            }
            if ((this.PasswordFormat == MembershipPasswordFormat.Hashed) && this.EnablePasswordRetrieval)
            {
                throw new ProviderException(ProviderResources.Provider_can_not_retrieve_hashed_password);
            }
            string str = config["passwordCompatMode"];
            if (!string.IsNullOrEmpty(str))
            {
                this.LegacyPasswordCompatibilityMode = (MembershipPasswordCompatibilityMode) Enum.Parse(typeof(MembershipPasswordCompatibilityMode), str);
            }
            config.Remove("applicationName");
            config.Remove("enablePasswordReset");
            config.Remove("enablePasswordRetrieval");
            config.Remove("maxInvalidPasswordAttempts");
            config.Remove("minRequiredNonalphanumericCharacters");
            config.Remove("minRequiredPasswordLength");
            config.Remove("passwordAttemptWindow");
            config.Remove("passwordFormat");
            config.Remove("passwordStrengthRegularExpression");
            config.Remove("requiresQuestionAndAnswer");
            config.Remove("requiresUniqueEmail");
            config.Remove("passwordCompatMode");
            if (config.Count > 0)
            {
                string key = config.GetKey(0);
                if (!string.IsNullOrEmpty(key))
                {
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_unrecognized_attribute, new object[] { key }));
                }
            }
        }

        private static bool IsStatusDueToBadPassword(int status)
        {
            return (((status >= 2) && (status <= 6)) || (status == 0x63));
        }

        private int Membership_CreateUser(string applicationName, string userName, string password, string salt, string email, string passwordQuestion, string passwordAnswer, bool isApproved, out DateTime createDate, bool uniqueEmail, int passwordFormat, ref object providerUserKey)
        {
            createDate = DateTime.UtcNow;
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                bool flag;
                bool createIfNotExist = true;
                Guid applicationId = QueryHelper.GetApplication(context, this.ApplicationName, createIfNotExist).ApplicationId;
                Guid? nullable = (Guid?) providerUserKey;
                User user = QueryHelper.GetUser(context, userName, applicationName);
                Guid? nullable2 = (user == null) ? null : new Guid?(user.UserId);
                if (!nullable2.HasValue)
                {
                    if (!nullable.HasValue)
                    {
                        nullable2 = new Guid?(Guid.NewGuid());
                    }
                    else
                    {
                        Guid userId = nullable.Value;
                        if (QueryHelper.GetUser(context, userId, applicationName) != null)
                        {
                            return 10;
                        }
                        nullable2 = new Guid?(nullable.Value);
                    }
                    ModelHelper.CreateUser(context, nullable2.Value, userName, applicationId, false);
                    flag = true;
                }
                else
                {
                    flag = false;
                    if (nullable.HasValue && (nullable2 != nullable.Value))
                    {
                        return 6;
                    }
                }
                if (QueryHelper.GetMembership(context, applicationName, nullable2.Value) != null)
                {
                    return 6;
                }
                if (uniqueEmail && (QueryHelper.GetUserNameFromEmail(context, email, applicationName) != null))
                {
                    return 7;
                }
                if (!flag)
                {
                    user.LastActivityDate = createDate;
                }
                MembershipEntity entity = new MembershipEntity {
                    ApplicationId = applicationId,
                    CreateDate = createDate,
                    Email = email,
                    FailedPasswordAnswerAttemptCount = 0,
                    FailedPasswordAnswerAttemptWindowsStart = NullDate,
                    FailedPasswordAttemptCount = 0,
                    FailedPasswordAttemptWindowStart = NullDate,
                    IsApproved = isApproved,
                    IsLockedOut = false,
                    LastLockoutDate = NullDate,
                    LastLoginDate = createDate,
                    LastPasswordChangedDate = createDate,
                    Password = password,
                    PasswordAnswer = passwordAnswer,
                    PasswordFormat = passwordFormat,
                    PasswordQuestion = passwordQuestion,
                    PasswordSalt = salt,
                    UserId = nullable2.Value
                };
                providerUserKey = nullable2.Value;
                context.Memberships.Add(entity);
                context.SaveChanges();
                return 0;
            }
        }

        public override string ResetPassword(string username, string answer)
        {
            if (!this.EnablePasswordReset)
            {
                throw new NotSupportedException(ProviderResources.Not_configured_to_support_password_resets);
            }
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "username";
            Exception exception = ValidationHelper.CheckParameter(ref username, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            if (answer != null)
            {
                answer = answer.Trim();
            }
            string password = Membership.GeneratePassword((this.MinRequiredPasswordLength < 14) ? 14 : this.MinRequiredPasswordLength, this.MinRequiredNonAlphanumericCharacters);
            ValidatePasswordEventArgs e = new ValidatePasswordEventArgs(username, password, false);
            this.OnValidatingPassword(e);
            if (e.Cancel)
            {
                if (e.FailureInformation != null)
                {
                    throw e.FailureInformation;
                }
                throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Membership_Custom_Password_Validation_Failure, new object[0]));
            }
            int status = 0;
            DateTime utcNow = DateTime.UtcNow;
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                MembershipEntity entity = QueryHelper.GetMembership(context, this.ApplicationName, username);
                if (entity == null)
                {
                    status = 1;
                }
                else if (entity.IsLockedOut)
                {
                    status = 0x63;
                }
                else
                {
                    string str2;
                    if (!string.IsNullOrEmpty(answer))
                    {
                        str2 = this.EncodePassword(answer, entity.PasswordFormat, entity.PasswordSalt);
                    }
                    else
                    {
                        str2 = answer;
                    }
                    bool requiresQuestionAndAnswer = this.RequiresQuestionAndAnswer;
                    bool flag5 = this.RequiresQuestionAndAnswer;
                    bool flag6 = false;
                    int num3 = 0x80;
                    string str4 = "answer";
                    exception = ValidationHelper.CheckParameter(ref str2, requiresQuestionAndAnswer, flag5, flag6, num3, str4);
                    if (exception != null)
                    {
                        throw exception;
                    }
                    if ((answer == null) || (string.Compare(entity.PasswordAnswer, str2, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        entity.Password = this.EncodePassword(password, entity.PasswordFormat, entity.PasswordSalt);
                        entity.LastPasswordChangedDate = DateTime.UtcNow;
                        if (entity.FailedPasswordAnswerAttemptCount > 0)
                        {
                            entity.FailedPasswordAnswerAttemptCount = 0;
                            entity.FailedPasswordAnswerAttemptWindowsStart = NullDate;
                        }
                    }
                    else
                    {
                        if (utcNow > entity.FailedPasswordAnswerAttemptWindowsStart.AddMinutes((double) this.PasswordAttemptWindow))
                        {
                            entity.FailedPasswordAnswerAttemptCount = 1;
                        }
                        else
                        {
                            entity.FailedPasswordAnswerAttemptCount++;
                        }
                        entity.FailedPasswordAnswerAttemptWindowsStart = utcNow;
                        if (entity.FailedPasswordAnswerAttemptCount >= this.MaxInvalidPasswordAttempts)
                        {
                            entity.IsLockedOut = true;
                            entity.LastLockoutDate = utcNow;
                        }
                        status = 3;
                    }
                    context.SaveChanges();
                }
            }
            ValidateStatus(status);
            return password;
        }

        private string UnEncodePassword(string pass, int passwordFormat)
        {
            switch (passwordFormat)
            {
                case 0:
                    return pass;

                case 1:
                    throw new ProviderException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Provider_can_not_decode_hashed_password, new object[0]));
            }
            byte[] encodedPassword = Convert.FromBase64String(pass);
            byte[] bytes = this.DecryptPassword(encodedPassword);
            if (bytes == null)
            {
                return null;
            }
            return Encoding.Unicode.GetString(bytes, 0x10, bytes.Length - 0x10);
        }

        public override bool UnlockUser(string userName)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "username";
            Exception exception = ValidationHelper.CheckParameter(ref userName, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                MembershipEntity entity = QueryHelper.GetMembership(context, this.ApplicationName, userName);
                if (entity != null)
                {
                    entity.IsLockedOut = false;
                    entity.FailedPasswordAnswerAttemptCount = 0;
                    entity.FailedPasswordAnswerAttemptWindowsStart = NullDate;
                    entity.FailedPasswordAttemptCount = 0;
                    entity.FailedPasswordAttemptWindowStart = NullDate;
                    entity.LastLockoutDate = NullDate;
                    context.SaveChanges();
                    return true;
                }
                return false;
            }
        }

        public override void UpdateUser(MembershipUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            string userName = user.UserName;
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = true;
            int maxSize = 0x100;
            string paramName = "user.UserName";
            Exception exception = ValidationHelper.CheckParameter(ref userName, checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
            if (exception != null)
            {
                throw exception;
            }
            string email = user.Email;
            bool requiresUniqueEmail = this.RequiresUniqueEmail;
            bool flag5 = this.RequiresUniqueEmail;
            bool flag6 = false;
            int num3 = 0x100;
            string str4 = "user.Email";
            exception = ValidationHelper.CheckParameter(ref email, requiresUniqueEmail, flag5, flag6, num3, str4);
            if (exception != null)
            {
                throw exception;
            }
            using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
            {
                int status = 0;
                Guid? providerUserKey = (Guid?) user.ProviderUserKey;
                if (!providerUserKey.HasValue)
                {
                    status = 1;
                }
                if (((status == 0) && this.RequiresUniqueEmail) && QueryHelper.DuplicateEmailExists(context, this.ApplicationName, providerUserKey.Value, user.Email))
                {
                    status = 7;
                }
                if (status == 0)
                {
                    QueryHelper.MembershipAndUser user2 = QueryHelper.GetMembershipAndUser(context, this.ApplicationName, providerUserKey.Value);
                    if (((user2 != null) && (user2.User != null)) && (user2.Membership != null))
                    {
                        user2.User.LastActivityDate = user.LastActivityDate.ToUniversalTime();
                        MembershipEntity membership = user2.Membership;
                        membership.Email = user.Email;
                        membership.Comment = user.Comment;
                        membership.IsApproved = user.IsApproved;
                        membership.LastLoginDate = user.LastLoginDate.ToUniversalTime();
                    }
                    context.SaveChanges();
                }
                else
                {
                    ValidateStatus(status);
                }
            }
        }

        private static void ValidateStatus(int status)
        {
            if (status != 0)
            {
                string exceptionText = GetExceptionText(status);
                if (IsStatusDueToBadPassword(status))
                {
                    throw new MembershipPasswordException(exceptionText);
                }
                throw new ProviderException(exceptionText);
            }
        }

        public override bool ValidateUser(string username, string password)
        {
            bool checkForNull = true;
            bool checkIfEmpty = true;
            bool checkForCommas = false;
            int maxSize = 0x80;
            if (ValidationHelper.ValidateParameter(ref password, checkForNull, checkIfEmpty, checkForCommas, maxSize))
            {
                bool flag6 = true;
                bool flag7 = true;
                bool flag8 = true;
                int num2 = 0x100;
                if (ValidationHelper.ValidateParameter(ref username, flag6, flag7, flag8, num2))
                {
                    using (MembershipContext context = ModelHelper.CreateMembershipContext(this.ConnectionString))
                    {
                        QueryHelper.MembershipAndUser user = QueryHelper.GetMembershipAndUser(context, this.ApplicationName, username);
                        if (user == null)
                        {
                            return false;
                        }
                        bool updateLastActivityDate = true;
                        bool failIfNotApproved = true;
                        bool flag = this.CheckPassword(user.Membership, user.User, password, updateLastActivityDate, failIfNotApproved);
                        context.SaveChanges();
                        return flag;
                    }
                }
            }
            return false;
        }

        public override string ApplicationName { get; set; }

        internal string ConfigHashAlgorithmType
        {
            get
            {
                return (this._configHashAlgorithmType ?? GetHashAlgorithmFromConfig());
            }
            set
            {
                this._configHashAlgorithmType = value;
            }
        }

        private ConnectionStringSettings ConnectionString { get; set; }

        public override bool EnablePasswordReset
        {
            get
            {
                return this.EnablePasswordResetInternal;
            }
        }

        internal bool EnablePasswordResetInternal { get; set; }

        public override bool EnablePasswordRetrieval
        {
            get
            {
                return this.EnablePasswordRetrievalInternal;
            }
        }

        internal bool EnablePasswordRetrievalInternal { get; set; }

        internal bool IsHashAlgorithmFromMembershipConfig
        {
            get
            {
                if (!this._hashAlgorithmFromConfig.HasValue)
                {
                    this._hashAlgorithmFromConfig = new bool?(!string.IsNullOrEmpty(this.ConfigHashAlgorithmType));
                }
                return this._hashAlgorithmFromConfig.Value;
            }
        }

        internal MembershipPasswordCompatibilityMode LegacyPasswordCompatibilityMode
        {
            get
            {
                return this._legacyPasswordCompatibilityMode;
            }
            set
            {
                this._legacyPasswordCompatibilityMode = value;
            }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get
            {
                return this.MaxInvalidPasswordAttemptsInternal;
            }
        }

        internal int MaxInvalidPasswordAttemptsInternal { get; set; }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get
            {
                return this.MinRequiredNonAlphanumericCharactersInternal;
            }
        }

        internal int MinRequiredNonAlphanumericCharactersInternal { get; set; }

        public override int MinRequiredPasswordLength
        {
            get
            {
                return this.MinRequiredPasswordLengthInternal;
            }
        }

        internal int MinRequiredPasswordLengthInternal { get; set; }

        public override int PasswordAttemptWindow
        {
            get
            {
                return this.PasswordAttemptWindowInternal;
            }
        }

        internal int PasswordAttemptWindowInternal { get; set; }

        public override MembershipPasswordFormat PasswordFormat
        {
            get
            {
                return this.PasswordFormatInternal;
            }
        }

        internal MembershipPasswordFormat PasswordFormatInternal { get; set; }

        public override string PasswordStrengthRegularExpression
        {
            get
            {
                return this.PasswordStrengthRegularExpressionInternal;
            }
        }

        internal string PasswordStrengthRegularExpressionInternal { get; set; }

        private string ProviderName
        {
            get
            {
                return this.Name;
            }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get
            {
                return this.RequiresQuestionAndAnswerInternal;
            }
        }

        internal bool RequiresQuestionAndAnswerInternal { get; set; }

        public override bool RequiresUniqueEmail
        {
            get
            {
                return this.RequiresUniqueEmailInternal;
            }
        }

        internal bool RequiresUniqueEmailInternal { get; set; }
    }
}

