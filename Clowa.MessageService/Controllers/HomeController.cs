using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.IO;
using System.Text;

namespace Clowa.MessageService.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://clowa.com");

            // Set some reasonable limits on resources used by this request
            request.MaximumAutomaticRedirections = 4;
            request.MaximumResponseHeadersLength = 4;
            // Set credentials to use for this request.
            CredentialCache myCache = new CredentialCache();
            myCache.Add(new Uri("http://clowa.com/service/login.ashx"), "Basic", new NetworkCredential("wyxy2005","13561387"));
            //myCache.Add(new Uri("http://www.contoso.com/"), "Digest", new NetworkCredential(UserName, SecurelyStoredPassword, Domain));

            request.Credentials = myCache;

            //request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Console.WriteLine("Content length is {0}", response.ContentLength);
            Console.WriteLine("Content type is {0}", response.ContentType);

            // Get the stream associated with the response.
            Stream receiveStream = response.GetResponseStream();

            // Pipes the stream to a higher level stream reader with the required encoding format. 
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

            Console.WriteLine("Response stream received.");
            Console.WriteLine(readStream.ReadToEnd());
            response.Close();
            readStream.Close();

            return View();
        }

        public DataSet RunSQLDS(string strSQL)
        {
            SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ClowaConnection"].ConnectionString);
            SqlCommand selectCommand = new SqlCommand(strSQL, connection);
            SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet);
            return dataSet;
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
