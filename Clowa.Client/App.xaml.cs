using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Practices.Prism;
using Clowa.Client.ViewModel;
using Clowa.Client.Interface;

namespace Clowa.Client
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Bootstrapper.Initialize();

            var loginViewModel = new LoginViewModel();
            var loginDialog = Service.ServiceProvider.Instance.Get<ILoginDialog>();
            loginDialog.BindViewModel(loginViewModel);
            loginDialog.Show();

        }
    }
}
