namespace System.Web.Providers
{
    using System;
    using System.Data.Common;
    using System.Data.Objects;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Web.Profile;
    using System.Web.Providers.Entities;
    using System.Web.Security;

    internal static class QueryHelper
    {
        private const string BaseGetAllMembershipsQuery = "select u.UserName, u.UserId, m.Email, m.PasswordQuestion, m.Comment, m.IsApproved, m.IsLockedOut, m.CreateDate, m.LastLoginDate, u.LastActivityDate, m.LastPasswordChangedDate, m.LastLockoutDate FROM Users as u, Memberships as m, Applications as a WHERE ToLower(a.ApplicationName) = @appName AND a.ApplicationId = m.ApplicationId AND m.UserId = u.UserId";

        internal static T Convert<T>(DbDataRecord record) where T: new()
        {
            T local = (default(T) == null) ? Activator.CreateInstance<T>() : default(T);
            for (int i = 0; i < record.FieldCount; i++)
            {
                PropertyInfo property = local.GetType().GetProperty(record.GetName(i));
                if (((property != null) && (property.PropertyType == record.GetFieldType(i))) && !record.IsDBNull(i))
                {
                    property.SetValue(local, record.GetValue(i), null);
                }
            }
            return local;
        }

        internal static bool DuplicateEmailExists(MembershipContext ctx, string applicationName, Guid userId, string email)
        {
            //return ((from m in ctx.Memberships
            //    join a in ctx.Applications on m.ApplicationId equals a.ApplicationId into a
            //    where ((a.ApplicationName.ToLower() == applicationName.ToLower()) && (m.Email.ToLower() == email.ToLower())) && (userId != m.UserId)
            //    select m).FirstOrDefault<MembershipEntity>() != null);
            return true;
        }

        internal static IQueryable<EFMembershipUser> GetAllMembershipUsers(MembershipContext ctx, string applicationName)
        {
            //return (from u in (from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    join m in ctx.Memberships on u.UserId equals m.UserId into m
            //    where a.ApplicationName.ToLower() == applicationName.ToLower()
            //    select new EFMembershipUser { UserName = u.UserName, UserId = u.UserId, Email = m.Email, PasswordQuestion = m.PasswordQuestion, Comment = m.Comment, IsApproved = m.IsApproved, IsLockedOut = m.IsLockedOut, CreateDate = m.CreateDate, LastLoginDate = m.LastLoginDate, LastActivityDate = u.LastActivityDate, LastPasswordChangedDate = m.LastPasswordChangedDate, LastLockoutDate = m.LastLockoutDate }).Distinct<EFMembershipUser>()
            //    orderby u.UserName
            //    select u);
            return null;
        }

        internal static IQueryable<DbDataRecord> GetAllMembershipUsersLikeEmail(MembershipContext ctx, string applicationName, string email)
        {
            //if (string.IsNullOrEmpty(email))
            //{
            //    string str = "select u.UserName, u.UserId, m.Email, m.PasswordQuestion, m.Comment, m.IsApproved, m.IsLockedOut, m.CreateDate, m.LastLoginDate, u.LastActivityDate, m.LastPasswordChangedDate, m.LastLockoutDate FROM Users as u, Memberships as m, Applications as a WHERE ToLower(a.ApplicationName) = @appName AND a.ApplicationId = m.ApplicationId AND m.UserId = u.UserId AND m.email = null ORDER BY u.UserName";
            //    return ctx.ObjectContext.CreateQuery<DbDataRecord>(str, new ObjectParameter[] { new ObjectParameter("appName", applicationName.ToLowerInvariant()) });
            //}
            //string queryString = "select u.UserName, u.UserId, m.Email, m.PasswordQuestion, m.Comment, m.IsApproved, m.IsLockedOut, m.CreateDate, m.LastLoginDate, u.LastActivityDate, m.LastPasswordChangedDate, m.LastLockoutDate FROM Users as u, Memberships as m, Applications as a WHERE ToLower(a.ApplicationName) = @appName AND a.ApplicationId = m.ApplicationId AND m.UserId = u.UserId AND ToLower(m.email) LIKE @email  ORDER BY u.UserName";
            //return ctx.ObjectContext.CreateQuery<DbDataRecord>(queryString, new ObjectParameter[] { new ObjectParameter("appName", applicationName.ToLowerInvariant()), new ObjectParameter("email", email.ToLowerInvariant()) });
            return null;
        }

        internal static IQueryable<DbDataRecord> GetAllMembershipUsersLikeUserName(MembershipContext ctx, string applicationName, string userName)
        {
            //string queryString = "select u.UserName, u.UserId, m.Email, m.PasswordQuestion, m.Comment, m.IsApproved, m.IsLockedOut, m.CreateDate, m.LastLoginDate, u.LastActivityDate, m.LastPasswordChangedDate, m.LastLockoutDate FROM Users as u, Memberships as m, Applications as a WHERE ToLower(a.ApplicationName) = @appName AND a.ApplicationId = m.ApplicationId AND m.UserId = u.UserId AND ToLower(u.UserName) LIKE @userName ORDER BY u.UserName";
            //return ctx.ObjectContext.CreateQuery<DbDataRecord>(queryString, new ObjectParameter[] { new ObjectParameter("appName", applicationName.ToLowerInvariant()), new ObjectParameter("userName", userName.ToLowerInvariant()) });
            return null;
        }

        internal static Application GetApplication(MembershipContext ctx, string applicationName, bool createIfNotExist = false)
        {
            Application application = (from a in ctx.Applications
                where a.ApplicationName.ToLower() == applicationName.ToLower()
                select a).FirstOrDefault<Application>();
            if ((application == null) && createIfNotExist)
            {
                application = ModelHelper.CreateApplication(ctx, applicationName);
            }
            return application;
        }

        internal static IQueryable<ProfileEntity> GetInactiveProfiles(MembershipContext ctx, string applicationName, ProfileAuthenticationOption authenticationOption, DateTime inactiveSinceDate)
        {
            //inactiveSinceDate = inactiveSinceDate.ToUniversalTime();
            //IQueryable<ProfileEntity> queryable = from p in ctx.Profiles
            //    join u in ctx.Users on p.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.LastActivityDate < inactiveSinceDate)
            //    select p;
            //switch (authenticationOption)
            //{
            //    case ProfileAuthenticationOption.Anonymous:
            //        return (from p in queryable
            //            where p.User.IsAnonymous
            //            select p);

            //    case ProfileAuthenticationOption.Authenticated:
            //        return (from p in queryable
            //            where !p.User.IsAnonymous
            //            select p);

            //    case ProfileAuthenticationOption.All:
            //        return queryable;
            //}
            //return queryable;
            return null;
        }

        internal static MembershipEntity GetMembership(MembershipContext ctx, string applicationName, Guid userId)
        {
            //return (from m in ctx.Memberships
            //    join u in ctx.Users on m.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserId == userId)
            //    select m).FirstOrDefault<MembershipEntity>();
            return null;
        }

        internal static MembershipEntity GetMembership(MembershipContext ctx, string applicationName, string userName)
        {
            //return (from m in ctx.Memberships
            //    join u in ctx.Users on m.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserName.ToLower() == userName.ToLower())
            //    select m).FirstOrDefault<MembershipEntity>();
            return null;
        }

        internal static MembershipAndUser GetMembershipAndUser(MembershipContext ctx, string applicationName, Guid userId)
        {
            //return (from m in ctx.Memberships
            //    join u in ctx.Users on m.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserId == userId)
            //    select new MembershipAndUser { User = u, Membership = m }).FirstOrDefault<MembershipAndUser>();
            return null;
        }

        internal static MembershipAndUser GetMembershipAndUser(MembershipContext ctx, string applicationName, string userName)
        {
            //return (from m in ctx.Memberships
            //    join u in ctx.Users on m.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserName.ToLower() == userName.ToLower())
            //    select new MembershipAndUser { User = u, Membership = m }).FirstOrDefault<MembershipAndUser>();
            return null;
        }

        internal static MembershipUser GetMembershipUser(MembershipContext ctx, Guid userId, string applicationName, bool userIsOnline, string providerName)
        {
            //IQueryable<EFMembershipUser> source = from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    join m in ctx.Memberships on u.UserId equals m.UserId into m
            //    where ((u.UserId == userId) && (a.ApplicationName.ToLower() == applicationName.ToLower())) && (u.ApplicationId == a.ApplicationId)
            //    select new EFMembershipUser { UserName = u.UserName, UserId = u.UserId, Email = m.Email, PasswordQuestion = m.PasswordQuestion, Comment = m.Comment, IsApproved = m.IsApproved, IsLockedOut = m.IsLockedOut, CreateDate = m.CreateDate, LastLoginDate = m.LastLoginDate, LastActivityDate = u.LastActivityDate, LastPasswordChangedDate = m.LastPasswordChangedDate, LastLockoutDate = m.LastLockoutDate };
            //if (userIsOnline)
            //{
            //    GetUser(ctx, userId, applicationName).LastActivityDate = DateTime.UtcNow;
            //    ctx.SaveChanges();
            //}
            //EFMembershipUser user2 = source.FirstOrDefault<EFMembershipUser>();
            //if (user2 != null)
            //{
            //    return user2.Convert(providerName);
            //}
            //return null;
            return null;
        }

        internal static MembershipUser GetMembershipUser(MembershipContext ctx, string username, string applicationName, bool userIsOnline, string providerName)
        {
            //EFMembershipUser user = (from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    join m in ctx.Memberships on u.UserId equals m.UserId into m
            //    where (username.ToLower() == u.UserName.ToLower()) && (a.ApplicationName.ToLower() == applicationName.ToLower())
            //    select new EFMembershipUser { UserName = u.UserName, UserId = u.UserId, Email = m.Email, PasswordQuestion = m.PasswordQuestion, Comment = m.Comment, IsApproved = m.IsApproved, IsLockedOut = m.IsLockedOut, CreateDate = m.CreateDate, LastLoginDate = m.LastLoginDate, LastActivityDate = u.LastActivityDate, LastPasswordChangedDate = m.LastPasswordChangedDate, LastLockoutDate = m.LastLockoutDate }).FirstOrDefault<EFMembershipUser>();
            //if (user == null)
            //{
            //    return null;
            //}
            //if (userIsOnline)
            //{
            //    User user2 = GetUser(ctx, username, applicationName);
            //    if (user2 != null)
            //    {
            //        user2.LastActivityDate = DateTime.UtcNow;
            //        ctx.SaveChanges();
            //    }
            //}
            //return user.Convert(providerName);
            return null;
        }

        internal static int GetNumberOfOnlineUsers(MembershipContext ctx, string applicationName, DateTime dateactive)
        {
            //ParameterExpression expression5;
            //return (from <>h__TransparentIdentifier30 in (from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId
            //    select new { u = u, a = a }).Where(
            //    Expression.Lambda(
            //    Expression.AndAlso(
            //    Expression.Equal(
            //    Expression.Call(
            //    Expression.Property(
            //    Expression.Property(
            //    expression5 = Expression.Parameter(
            //        typeof(<>f__AnonymousType7<User,Application><User, Application>), "<>h__TransparentIdentifier30"),
            //            (MethodInfo) methodof(<>f__AnonymousType7<User,Application><User,
            //            Application>.get_a, <>f__AnonymousType7<User,Application><User, Application>)), 
            //                (MethodInfo) methodof(Application.get_ApplicationName)), 
            //                    (MethodInfo) methodof(string.ToLower), new Expression[0]), 
            //Expression.Call(Expression.Constant(applicationName), (MethodInfo) methodof(string.ToLower), 
            //    new Expression[0]), false, (MethodInfo) methodof(string.op_Equality)), 
            //Expression.GreaterThan(Expression.Property(Expression.Property(expression5, 
            //    (MethodInfo) methodof(<>f__AnonymousType7<User,Application><User, Application>.get_u,
            //        <>f__AnonymousType7<User,Application><User, Application>)), 
            //            (MethodInfo) methodof(User.get_LastActivityDate)), 
            //Expression.Constant(dateactive), false, (MethodInfo) methodof(DateTime.op_GreaterThan))),
            //new ParameterExpression[] { expression5 })) select <>h__TransparentIdentifier30.u).Count<User>();
            return 0;
        }

        internal static ProfileEntity GetProfile(MembershipContext ctx, string applicationName, string username)
        {
            //return (from p in ctx.Profiles
            //    join u in ctx.Users on p.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserName.ToLower() == username.ToLower())
            //    select p).FirstOrDefault<ProfileEntity>();
            return null;
        }

        internal static ProfileAndUser GetProfileAndUser(MembershipContext ctx, string applicationName, string username)
        {
            //return (from p in ctx.Profiles
            //    join u in ctx.Users on p.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserName.ToLower() == username.ToLower())
            //    select new ProfileAndUser { Profile = p, User = u }).FirstOrDefault<ProfileAndUser>();
            return null;
        }

        internal static IQueryable<EFProfileInfo> GetProfileInfos(MembershipContext ctx, string applicationName, ProfileAuthenticationOption authenticationOption, 
            DateTime inactiveSinceDate, string userNameContains)
        {
            //IQueryable<EFProfileInfo> queryable = from p in ctx.Profiles
            //    join u in ctx.Users on p.UserId equals u.UserId into u
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where a.ApplicationName.ToLower() == applicationName.ToLower()
            //    orderby h__TransparentIdentifier51.<>h__TransparentIdentifier50.u.UserName
            //    select new EFProfileInfo { UserId = u.UserId, UserName = u.UserName, IsAnonymous = u.IsAnonymous, LastActivityDate = u.LastActivityDate, LastUpdatedDate = p.LastUpdatedDate, PropertyValueBinary = p.PropertyValueBinary, PropertyNames = p.PropertyNames, PropertyValueStrings = p.PropertyValueStrings };
            //switch (authenticationOption)
            //{
            //    case ProfileAuthenticationOption.Anonymous:
            //        queryable = from u in queryable
            //            where u.IsAnonymous
            //            select u;
            //        break;

            //    case ProfileAuthenticationOption.Authenticated:
            //        queryable = from u in queryable
            //            where !u.IsAnonymous
            //            select u;
            //        break;
            //}
            //if (inactiveSinceDate != DateTime.MaxValue)
            //{
            //    inactiveSinceDate = inactiveSinceDate.ToUniversalTime();
            //    queryable = from u in queryable
            //        where u.LastActivityDate < inactiveSinceDate
            //        select u;
            //}
            //if (!string.IsNullOrEmpty(userNameContains))
            //{
            //    queryable = from u in queryable
            //        where u.UserName.ToLower().Contains(userNameContains.ToLower())
            //        select u;
            //}
            //return queryable;
            return null;
        }

        internal static RoleEntity GetRole(MembershipContext ctx, string roleName, Guid applicationId)
        {
            return (from r in ctx.Roles
                where (r.ApplicationId == applicationId) && (r.RoleName.ToLower() == roleName.ToLower())
                select r).FirstOrDefault<RoleEntity>();
        }

        internal static RoleEntity GetRole(MembershipContext ctx, string roleName, string applicationName)
        {
            //return (from r in ctx.Roles
            //    join a in ctx.Applications on r.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (r.RoleName.ToLower() == roleName.ToLower())
            //    select r).FirstOrDefault<RoleEntity>();
            return null;
        }

        internal static string[] GetRolesNamesForUser(MembershipContext ctx, string applicationName, string username)
        {
            //return (from r in ctx.Roles
            //    join usr in ctx.UsersInRoles on r.RoleId equals usr.RoleId into usr
            //    join u in ctx.Users on usr.UserId equals u.UserId into u
            //    join a in ctx.Applications on r.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (username.ToLower() == u.UserName.ToLower())
            //    orderby <>h__TransparentIdentifier3c.<>h__TransparentIdentifier3b.<>h__TransparentIdentifier3a.r.RoleName
            //    select r.RoleName).ToArray<string>();
            return null;
        }

        internal static User GetUser(MembershipContext ctx, Guid userId, string applicationName)
        {
            //return (from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserId == userId)
            //    select u).FirstOrDefault<User>();
            return null;
        }

        internal static User GetUser(MembershipContext ctx, string userName, string applicationName)
        {
            //return (from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserName.ToLower() == userName.ToLower())
            //    select u).FirstOrDefault<User>();
            return null;
        }

        internal static User GetUser(MembershipContext ctx, string userName, Application application)
        {
            return (from u in ctx.Users
                where (u.ApplicationId == application.ApplicationId) && (u.UserName.ToLower() == userName.ToLower())
                select u).FirstOrDefault<User>();
        }

        internal static Guid GetUserIdFromUserName(MembershipContext ctx, string userName, string applicationName)
        {
            //return (from u in ctx.Users
            //    join a in ctx.Applications on u.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (u.UserName.ToLower() == userName.ToLower())
            //    select u.UserId).FirstOrDefault<Guid>();
            return Guid.NewGuid();
        }

        internal static UsersInRole GetUserInRole(MembershipContext ctx, Guid userId, Guid roleId)
        {
            return (from usr in ctx.UsersInRoles
                where (usr.UserId == userId) && (usr.RoleId == roleId)
                select usr).FirstOrDefault<UsersInRole>();
        }

        internal static string GetUserNameFromEmail(MembershipContext ctx, string email, string applicationName)
        {
            //return (from m in ctx.Memberships
            //    join u in ctx.Users on m.UserId equals u.UserId into u
            //    join a in ctx.Applications on m.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (m.Email.ToLower() == email.ToLower())
            //    select u.UserName).FirstOrDefault<string>();
            return null;
        }

        internal static IQueryable<UsersInRole> GetUserRolesForUser(MembershipContext ctx, string applicationName, string username)
        {
            //return (from r in ctx.Roles
            //    join usr in ctx.UsersInRoles on r.RoleId equals usr.RoleId into usr
            //    join u in ctx.Users on usr.UserId equals u.UserId into u
            //    join a in ctx.Applications on r.ApplicationId equals a.ApplicationId into a
            //    where (a.ApplicationName.ToLower() == applicationName.ToLower()) && (username.ToLower() == u.UserName.ToLower())
            //    orderby <>h__TransparentIdentifier41.<>h__TransparentIdentifier40.<>h__TransparentIdentifier3f.r.RoleName
            //    select usr);
            return null;
        }

        internal class EFMembershipUser
        {
            public MembershipUser Convert(string providerName)
            {
                return new MembershipUser(providerName, this.UserName, this.UserId, this.Email, this.PasswordQuestion, this.Comment, this.IsApproved, this.IsLockedOut, DateTime.SpecifyKind(this.CreateDate, DateTimeKind.Utc), DateTime.SpecifyKind(this.LastLoginDate, DateTimeKind.Utc), DateTime.SpecifyKind(this.LastActivityDate, DateTimeKind.Utc), DateTime.SpecifyKind(this.LastPasswordChangedDate, DateTimeKind.Utc), DateTime.SpecifyKind(this.LastLockoutDate, DateTimeKind.Utc));
            }

            public string Comment { get; set; }

            public DateTime CreateDate { get; set; }

            public string Email { get; set; }

            public bool IsApproved { get; set; }

            public bool IsLockedOut { get; set; }

            public DateTime LastActivityDate { get; set; }

            public DateTime LastLockoutDate { get; set; }

            public DateTime LastLoginDate { get; set; }

            public DateTime LastPasswordChangedDate { get; set; }

            public string PasswordQuestion { get; set; }

            public Guid UserId { get; set; }

            public string UserName { get; set; }
        }

        internal class EFProfileInfo
        {
            public ProfileInfo ToProfileInfo()
            {
                return new ProfileInfo(this.UserName, this.IsAnonymous, this.LastActivityDate.ToLocalTime(), this.LastUpdatedDate.ToLocalTime(), (this.PropertyNames.Length + this.PropertyValueStrings.Length) + this.PropertyValueBinary.Length);
            }

            public bool IsAnonymous { get; set; }

            public DateTime LastActivityDate { get; set; }

            public DateTime LastUpdatedDate { get; set; }

            public string PropertyNames { get; set; }

            public byte[] PropertyValueBinary { get; set; }

            public string PropertyValueStrings { get; set; }

            public Guid UserId { get; set; }

            public string UserName { get; set; }
        }

        internal class MembershipAndUser
        {
            public MembershipEntity Membership { get; set; }

            public System.Web.Providers.Entities.User User { get; set; }
        }

        internal class ProfileAndUser
        {
            public ProfileEntity Profile { get; set; }

            public System.Web.Providers.Entities.User User { get; set; }
        }
    }
}

