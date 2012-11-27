using System;
using System.Net;
using System.Threading.Tasks;

namespace Clowa.Client
{
    public static class LoginHelper
    {
        /// <summary>
        /// Logs into the MessengR service using forms auth and retrives an auth cookie.
        /// </summary>
        public static Task<AuthenticationResult> LoginAsync(string url, string userName, string password)
        {
            // We're going to login and retrieve the auth token
            var uri = new Uri(new Uri(url, UriKind.Absolute), "service/Login.ashx");
            var webRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
            //webRequest.PreAuthenticate = true;
            //webRequest.KeepAlive = true;
            webRequest.Credentials = new NetworkCredential(userName, password);
            //CredentialCache myCache = new CredentialCache();
            //myCache.Add(uri, "Basic", new NetworkCredential(userName, password));
            //webRequest.Credentials = myCache;
            webRequest.CookieContainer = new CookieContainer();
            var tcs = new TaskCompletionSource<AuthenticationResult>();
            webRequest.BeginGetResponse(ar =>
            {
                var authResult = new AuthenticationResult()
                {
                    StatusCode = HttpStatusCode.Unused
                };

                try
                {
                    using (var response = (HttpWebResponse)webRequest.EndGetResponse(ar))
                    {
                        authResult.StatusCode = response.StatusCode;
                        authResult.AuthCookie = response.Cookies[".ASPXAUTH"];
                    }
                }
                catch (WebException ex)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        authResult.StatusCode = response.StatusCode;
                        authResult.Error = ex.Message;
                    }
                    else
                    {
                        authResult.Error = ex.Message;
                    }
                }
                catch (Exception ex)
                {
                    authResult.Error = ex.Message;
                }

                tcs.SetResult(authResult);
            },
            null);

            return tcs.Task;
        } 
    }
}
