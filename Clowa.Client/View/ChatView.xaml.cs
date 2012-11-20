using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Specialized;

namespace Clowa.Client.View
{
    /// <summary>
    /// ChatView.xaml 的交互逻辑
    /// </summary>
    public partial class ChatView
    {
        public ChatView()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)ConversationList.Items).CollectionChanged += ChatView_CollectionChanged;
        }

        void ChatView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                ConversationList.ScrollIntoView(e.NewItems[0]);
            }
        }
    }
}
