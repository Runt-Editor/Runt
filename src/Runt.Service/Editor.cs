using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Runt.Core;
using Runt.Core.Model;
using System.Linq;

namespace Runt.Service
{
    public class Editor : IEditor
    {
        private EditorState _state = EditorState.Null;

        public IClientConnection ClientConnection { get; set; }

        public void NotifyConnected()
        {
            Send(Messages.State(_state));
        }

        public void NotifyReceived(string data)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<Command>(data);
                Invoke(msg);
            }
            catch(Exception e)
            {
                Send(Messages.Error(e));
            }
        }

        private void Send(string message)
        {
            var conn = ClientConnection;
            if (conn != null)
                conn.Send(message);
        }

        private void Invoke(Command command)
        {
            var type = GetType();
            var methods = type.GetMethods();
            object[] args = new object[0];
            var method = methods.Single(m =>
            {
                var attr = (CommandAttribute)m.GetCustomAttributes(typeof(CommandAttribute), false).SingleOrDefault();
                if (attr == null)
                    return false;

                var name = attr.Name;
                if (name != command.Name)
                    return false;

                var p = m.GetParameters();
                if (p.Length != command.Arguments.Count)
                    return false;

                try
                {
                    args = new object[p.Length];
                    for(var i = 0; i < p.Length; i++)
                        args[i] = command.Arguments[i].ToObject(p[i].ParameterType);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
            method.Invoke(this, args);
        }

        void Update(Func<EditorState, EditorState> change)
        {
            EditorState newState;
            while (true)
            {
                var original = Volatile.Read(ref _state);
                newState = change(original);
                if (ReferenceEquals(Interlocked.CompareExchange(ref _state, newState, original), original))
                    break;
            }

            Send(Messages.State(newState));
        }

        [Command("browse-project")]
        public void BrowseProject()
        {
            Update(Utils.Update((EditorState s) => s.WithDialog(Dialog.Browse())));
        }

        [Command("browse-project")]
        public void BrowseProject(string path)
        {
            Update(Utils.Update((EditorState s) => s.WithDialog(Dialog.Browse(path))));
        }
    }
}
