using System;
using Clowa.Models;

namespace Clowa.Client.Events
{
    public class OpenChatEventArgs : EventArgs
    {
        public User Contact { get; set; }

        public OpenChatEventArgs(User contact)
        {
            Contact = contact;
        }
    }
}
