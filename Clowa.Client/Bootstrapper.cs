using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clowa.Client.Service;
using Clowa.Client.Interface;
using Clowa.Client.View;

namespace Clowa.Client
{
    class Bootstrapper
    {
        public static void Initialize()
        { 
            ServiceProvider.RegisterServiceLocator(new UnityServiceLocator());

            ServiceProvider.Instance.Register<IChatDialog, ChatViewDialog>();
            ServiceProvider.Instance.Register<ILoginDialog, LoginViewDialog>();
        } 
    }
}
