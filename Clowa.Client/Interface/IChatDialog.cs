using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clowa.Client.Events;

namespace Clowa.Client.Interface
{
    public interface IChatDialog : IDialog
    {
        event EventHandler<ChatSessionEventArgs> ViewClosedEvent;
    }
}
