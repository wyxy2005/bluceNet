namespace MembershipService.WebForms
{
    using System;
    using System.Runtime.CompilerServices;

    public class HttpResult
    {
        public static readonly HttpResult OK = new HttpResult(200, "OK", null);

        public HttpResult(int statusCode, string statusDescription, string content)
        {
            this.StatusCode = statusCode;
            this.StatusDescription = statusDescription;
            this.Content = content;
        }

        public string Content { get; private set; }

        public int StatusCode { get; private set; }

        public string StatusDescription { get; private set; }
    }
}

