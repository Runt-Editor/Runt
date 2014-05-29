using System;
using System.Threading.Tasks;
using Runt.Core;

namespace Runt.Service
{
    public class Editor : IEditor
    {
        public IClientConnection ClientConnection { get; set; }

        public void NotifyConnected()
        {
            ClientConnection.Send("hello");

            Task.Run(async () =>
            {
                for (var i = 0; i < 10; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    await ClientConnection.Send("ping: " + i);
                }
            });
        }

        public void NotifyReceived(string data)
        {
            ClientConnection.Send("Pong: " + data);
        }
    }
}
