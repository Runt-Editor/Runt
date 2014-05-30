using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json.Linq;
using Runt.Core;

namespace Runt.Web
{
    public class RuntConnection : PersistentConnection
    {
        readonly IServiceProvider _serviceProvider;
        readonly ConcurrentDictionary<string, IEditor> _editors = new ConcurrentDictionary<string, IEditor>();

        public RuntConnection(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            GetEditor(connectionId).NotifyConnected();
            return base.OnConnected(request, connectionId);
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            GetEditor(connectionId).NotifyReceived(data);
            return base.OnReceived(request, connectionId, data);
        }

        private IEditor GetEditor(string connectionId)
        {
            return _editors.GetOrAdd(connectionId, key =>
            {
                var editor = (IEditor)_serviceProvider.GetService(typeof(IEditor));
                editor.ClientConnection = new ClientConnection(this, connectionId);
                return editor;
            });
        }

        class ClientConnection : IClientConnection
        {
            readonly RuntConnection _conn;
            readonly string _id;

            public ClientConnection(RuntConnection connection, string connectionId)
            {
                _conn = connection;
                _id = connectionId;
            }

            public Task Send(string message)
            {
                return _conn.Connection.Send(_id, new JRaw(message));
            }
        }
    }
}