using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace Clowa.MessageService.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var ds = RunSQLDS("select * from sys.databases where name ='clowa'");
            int i = 0;
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                foreach (DataColumn drc in dr.Table.Columns)
                {
                    Response.Write("\r\n<!-- rowIdx=" + i + " ColumnName=" + drc.ColumnName +" Value="+ dr[drc.ColumnName].ToString() + "-->");
                }
                i=i++;
            }
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
