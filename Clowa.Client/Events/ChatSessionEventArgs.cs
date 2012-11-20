using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clowa.Models;

namespace Clowa.Client.Events
{
  public class ChatSessionEventArgs : EventArgs
    {
        public User Contact { get; set; }
        public string Message { get; set; }

        public ChatSessionEventArgs(User user, string message)
        {
            Contact = user;
            Message = message;
        }
    }
}
