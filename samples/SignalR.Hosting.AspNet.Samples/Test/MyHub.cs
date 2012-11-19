using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SignalR.Hubs;
using System.Threading.Tasks;
using System.Threading;

namespace SignalR.Hosting.AspNet.Samples.Test
{
    [HubName("My")]
    public class MyHub : Hub, IDisconnect
    {
        public Task Join()
        {
            return Groups.Add(Context.ConnectionId, "foo");
        }

        public Task Send(string message)
        {
            return Clients["foo"].addMessage(message);
        }

        public Task Disconnect()
        {
            return Clients["foo"].leave(Context.ConnectionId);
        } 
    }

    public class Notifier
    {
        public static void Say(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            context.Clients.say(message);
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

}