using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using System;
using System.Web;
using System.Web.Security;

namespace MembershipService.Common
{
    public class DoNotRedirectToLoginModule : IHttpModule
    {
        internal static readonly object DoNotRedirectToLoginKey = new object();

        public static void ApplyForRequest(HttpContextBase httpContext)
        {
            httpContext.Items[DoNotRedirectToLoginKey] = true;
        }

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.EndRequest += delegate(object sender, EventArgs eventArgs)
            {
                if (((bool?)HttpContext.Current.Items[DoNotRedirectToLoginKey]) == true)
                {
                    HttpResponse response = HttpContext.Current.Response;
                    if ((response.StatusCode == 0x12e) && response.RedirectLocation.StartsWith(FormsAuthentication.LoginUrl + "?"))
                    {//0x12e=302
                        response.ClearContent();
                        response.StatusCode = 0x191;//0x191=401
                    }
                }
            };
        }

        public static void Register()
        {
            DynamicModuleUtility.RegisterModule(typeof(DoNotRedirectToLoginModule));
        }
    }
}
