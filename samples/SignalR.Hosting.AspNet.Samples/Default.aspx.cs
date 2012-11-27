using System;

namespace SignalR.Samples
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            this.Session.Add("test", "bbbbbb");
            Response.Write("<!-- test " + this.Session["test"].ToString() + "-->");
        }
    }
}
