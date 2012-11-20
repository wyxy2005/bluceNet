using System.ComponentModel;
using System.Configuration;

namespace Clowa.Client.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public readonly string HostUrl = ConfigurationManager.AppSettings["HostUrl"];
        #region Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
