using System;
using System.Web.Security;
using MembershipService.WebForms;

namespace $rootnamespace$
{
    public class MembershipService : SimpleHttpHandler
    {
        public string LoggedOnAs()
        {
            return User.Identity.Name;
        }

        public HttpResult Register(string name, string password, string email, string passwordQuestion, string passwordAnswer)
        {
            EnsureHttpMethod("POST");

            MembershipCreateStatus createStatus;
            Membership.CreateUser(name, password, email, passwordQuestion, passwordAnswer, isApproved: true, providerUserKey: null, status: out createStatus);

            if (createStatus != MembershipCreateStatus.Success)
                return Result(400, null, createStatus.ToString());

            return HttpResult.OK;
        }

        public HttpResult LogOn(string name, string password)
        {
            EnsureHttpMethod("POST");

            if (Membership.ValidateUser(name, password)) {
                FormsAuthentication.SetAuthCookie(name, createPersistentCookie: false);
                return HttpResult.OK;
            }

            return Result(401);
        }

        public HttpResult LogOut()
        {
            FormsAuthentication.SignOut();
            return HttpResult.OK;
        }

        public string Roles()
        {
            // Returns a list of roles for the current visitor, one role per line
            return string.Join(Environment.NewLine, System.Web.Security.Roles.GetRolesForUser());
        }

        public HttpResult Profile(string key, string value)
        {
            if (Request.HttpMethod == "POST") {
                // Write the value
                HttpContext.Profile[key] = value;
                return HttpResult.OK;
            }

            // Read the value
            object propertyValue = HttpContext.Profile[key];
            return Result(propertyValue == null ? string.Empty : propertyValue.ToString());
        }
    }
}