using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Clowa.Client
{
    public class AuthenticationResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Error { get; set; }
        public Cookie AuthCookie { get; set; }
    }
}
