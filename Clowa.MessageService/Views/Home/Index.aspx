<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Clowa.com
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<%  
var url = Regex.Replace(Request.Url.AbsoluteUri, @"Index\.aspx", "", RegexOptions.IgnoreCase);
var config =String.Format(@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
    <add key=""HostUrl"" value=""{0}""/>
  </appSettings>
</configuration>""", url);
%>
    <p>
     <ol>
         <% if (Request.IsAuthenticated)
            {%>
        
              <li>
              Change the settings in Client's app.config
                <pre><%: config%></pre>
              </li>
        
          <%}
            else
            {%><li> Clowa is a Tester  Message Site!</li>
          <%} %>
           </ol>
    </p>

</asp:Content>
