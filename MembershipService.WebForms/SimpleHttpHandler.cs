namespace MembershipService.WebForms
{
    using MembershipService.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Principal;
    using System.Web;

    public class SimpleHttpHandler : IHttpHandler
    {
        private static readonly List<Type> _actionsExcludeMethodsOnTypes = GetTypeAncestry(typeof(SimpleHttpHandler)).ToList<Type>();

        protected void EnsureHttpMethod(string httpMethod)
        {
            if (!this.Request.HttpMethod.Equals(httpMethod, StringComparison.OrdinalIgnoreCase))
            {
                this.Response.StatusCode = 0x194;
                this.Response.End();
            }
        }

        private MethodInfo FindMethodToInvoke(System.Web.HttpContext context)
        {
            string currentExecutionFilePath = context.Request.CurrentExecutionFilePath;
            string absolutePath = context.Request.Url.AbsolutePath;
            if (!absolutePath.StartsWith(currentExecutionFilePath + "/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SimpleHttpHandler cannot determine action because URL does not match handler path");
            }
            string action = absolutePath.Substring(currentExecutionFilePath.Length + 1);
            MethodInfo info = this.GetActions().SingleOrDefault<MethodInfo>(m => m.Name.Equals(action, StringComparison.OrdinalIgnoreCase));
            if (info == null)
            {
                throw new HttpException(0x194, "Not found");
            }
            return info;
        }

        private IEnumerable<MethodInfo> GetActions()
        {
            return (from method in base.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                where !method.IsSpecialName && !_actionsExcludeMethodsOnTypes.Contains(method.DeclaringType)
                select method);
        }

        private object GetParameterValueFromHttpContext(ParameterInfo parameterInfo)
        {
            string str = this.Request[parameterInfo.Name];
            return ((str != null) ? Convert.ChangeType(str, parameterInfo.ParameterType) : null);
        }

        private object[] GetParameterValuesFromHttpContext(MethodInfo method)
        {
            return method.GetParameters().Select<ParameterInfo, object>(new Func<ParameterInfo, object>(this.GetParameterValueFromHttpContext)).ToArray<object>();
        }

        private static IEnumerable<Type> GetTypeAncestry(Type type)
        {
            Type[] second = new Type[] { type };
            return ((type.BaseType != null) ? GetTypeAncestry(type.BaseType).Concat<Type>(second) : ((IEnumerable<Type>) second));
        }

        private void InvokeHttpResult(HttpResult httpResult)
        {
            if (httpResult.StatusCode != 0)
            {
                this.Response.StatusCode = httpResult.StatusCode;
            }
            if (httpResult.StatusDescription != null)
            {
                this.Response.StatusDescription = httpResult.StatusDescription;
            }
            if (httpResult.Content != null)
            {
                this.Response.Write(httpResult.Content);
            }
        }

        private void InvokeResult(object methodResult)
        {
            this.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            HttpResult httpResult = methodResult as HttpResult;
            if (httpResult != null)
            {
                this.InvokeHttpResult(httpResult);
            }
            else if (methodResult != null)
            {
                this.Response.Write(methodResult.ToString());
            }
        }

        public void ProcessRequest(System.Web.HttpContext context)
        {
            this.HttpContext = new HttpContextWrapper(context);
            DoNotRedirectToLoginModule.ApplyForRequest(this.HttpContext);
            MethodInfo method = this.FindMethodToInvoke(context);
            object[] parameterValuesFromHttpContext = this.GetParameterValuesFromHttpContext(method);
            this.InvokeResult(method.Invoke(this, parameterValuesFromHttpContext));
        }

        public HttpResult Result(int statusCode)
        {
            return this.Result(statusCode, null, null);
        }

        public HttpResult Result(string content)
        {
            return this.Result(200, "OK", content);
        }

        public HttpResult Result(int statusCode, string statusDescription, string content)
        {
            return new HttpResult(statusCode, statusDescription, content);
        }

        protected HttpContextBase HttpContext { get; private set; }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        protected HttpRequestBase Request
        {
            get
            {
                return this.HttpContext.Request;
            }
        }

        protected HttpResponseBase Response
        {
            get
            {
                return this.HttpContext.Response;
            }
        }

        protected IPrincipal User
        {
            get
            {
                return this.HttpContext.User;
            }
        }
    }
}

