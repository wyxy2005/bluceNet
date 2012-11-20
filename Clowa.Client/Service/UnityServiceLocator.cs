using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using Clowa.Client.Interface;

namespace Clowa.Client.Service
{
    public class UnityServiceLocator : IServiceLocator
    {
        private UnityContainer _container;
        public UnityServiceLocator()
        {
            _container = new UnityContainer();
        }
        #region Implementation of IServiceLocator
        void IServiceLocator.Register<TInterface, TImplementation>()
        {
            _container.RegisterType<TInterface, TImplementation>();
        }

        TInterface IServiceLocator.Get<TInterface>()
        {
            return _container.Resolve<TInterface>();
        } 
        #endregion
    }
}
