using System;
using System.Threading;
using System.Threading.Tasks;
using Runt.Core;
using Runt.Core.Model;

namespace Runt.Service
{
    public class Editor : IEditor
    {
        private EditorState _state;

        public IClientConnection ClientConnection { get; set; }

        public void NotifyConnected()
        {
            Send(Messages.State(_state));
        }

        public void NotifyReceived(string data)
        {
            
        }

        private void Send(string message)
        {
            var conn = ClientConnection;
            if (conn != null)
                conn.Send(message);
        }

        static void Update(ref EditorState state, Func<EditorState, EditorState> change)
        {
            while(true)
            {
                var original = Volatile.Read(ref state);
                var newState = change(original);
                if (ReferenceEquals(Interlocked.CompareExchange(ref state, newState, original), original))
                    return;
            }
        }
    }
}
