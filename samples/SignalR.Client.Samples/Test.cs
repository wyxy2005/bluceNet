using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Client.Hubs;
using System.Threading;

namespace SignalR.Client.Samples
{
    class Test
    {
        static void Main(string[] args)
        {

            //輸入識別名稱

            Console.Write("Please input client name: ");

            string clientName = Console.ReadLine();

            //連線SignalR Hub

            var connection = new HubConnection("http://localhost:4112/");

            IHubProxy commHub = connection.CreateProxy("CommHub");

            //顯示Hub傳入的文字訊息

            commHub.On("ShowMessage", msg => Console.WriteLine(msg));

            //利用done旗標決定程式中止

            bool done = false;

            //當Hub要求執行Exit()時，將done設為true

            commHub.On("Exit", () => { done = true; });

            //建立連線，連線建立完成後向Hub註冊識別名稱

            connection.Start().ContinueWith(task =>
            {

                if (!task.IsFaulted)

                    //連線成功時呼叫Server端方法register()

                    commHub.Invoke("register", clientName);

                else

                    done = true;

            });

            //維持程式執行迴圈

            while (!done)
            {

                Thread.Sleep(100);

            }

            //主動結束連線 
            connection.Stop();

        }
    }
}
