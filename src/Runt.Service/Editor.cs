using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Runt.Core;
using Runt.Core.Model;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using Runt.DesignTimeHost;
using Runt.Core.Model.FileTree;
using System.Collections.Generic;
using System.Diagnostics;

namespace Runt.Service
{
    public class Editor : IEditor
    {
        private EditorState _state = EditorState.Null;
        private FileSystemWatcher _watcher;
        private Host _host;
        private int _nextId;

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
            catch (Exception e)
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
                    for (var i = 0; i < p.Length; i++)
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

        void InitializeProjects(EditorState state)
        {
            if (state.Workspace != null)
            {
                List<Tuple<ProjectEntry, int>> update = new List<Tuple<ProjectEntry, int>>();
                foreach (var project in state.Workspace.Projects)
                {
                    if (!project.Initialized)
                    {
                        int id = Interlocked.Increment(ref _nextId);
                        update.Add(new Tuple<ProjectEntry, int>(project, id));
                        if (_host != null)
                            _host.InitProject(id, project.Path);
                    }
                }

                if (update.Count > 0)
                {
                    var updateFns = new List<Tuple<string, Func<ProjectEntry, JObject, ProjectEntry>>>();
                    foreach (var p in update)
                    {
                        updateFns.Add(new Tuple<string, Func<ProjectEntry, JObject, ProjectEntry>>(
                            p.Item1.RelativePath, (ProjectEntry pr, JObject c) =>
                                pr.WithId(p.Item2, c)));
                    }

                    UpdateNodes(updateFns);
                }
            }
        }

        void UpdateNode<TNodeType>(string relPath, Func<TNodeType, JObject, TNodeType> update)
            where TNodeType : Entry
        {
            UpdateNodes(new[] { new Tuple<string, Func<TNodeType, JObject, TNodeType>>(relPath, update) });
        }

        void UpdateNodes<TNodeType>(IEnumerable<Tuple<string, Func<TNodeType, JObject, TNodeType>>> updates)
            where TNodeType : Entry
        {
            Update(Utils.Update((EditorState s, JObject c) =>
            {
                var contentC = new JObject();
                var workspaceC = new JObject();
                var workspace = s.Workspace;
                var content = workspace.Content;
                foreach (var update in updates)
                    content = UpdateNodeImpl(content, update.Item1, update.Item2, contentC);

                workspace = workspace.WithContent(content, workspaceC, contentC);
                return s.WithWorkspace(workspace, c, workspaceC);
            }));
        }

        Entry UpdateNodeImpl<TNodeType>(Entry entry, string relPath, Func<TNodeType, JObject, TNodeType> update, JObject c)
            where TNodeType : Entry
        {
            if (relPath == string.Empty)
                return update((TNodeType)entry, c);

            var subChange = new JObject();
            Entry newEntry;
            for (int i = 0, l = entry.Children.Count; i < l; i++)
            {
                var child = entry.Children[i];
                if (relPath.Equals(child.RelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    // found the node. Update and recurse back
                    TNodeType n = (TNodeType)child;
                    newEntry = update(n, subChange);

                    if(!newEntry.IsOpen && subChange.Property("children") != null)
                    {
                        var childrenUpdate = subChange.Property("children").Value;
                        if (childrenUpdate is JArray)
                            ((JArray)childrenUpdate).Clear();
                        else
                            ((JObject)childrenUpdate).RemoveAll();
                    }

                    return entry.WithChild(i, newEntry, c, subChange);

                }
                else if (relPath.StartsWith(child.RelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    // found a parent of the node in question, recurse downwards
                    newEntry = UpdateNodeImpl(child, relPath, update, subChange);
                    if (!child.IsOpen)
                        subChange = new JObject();
                    return entry.WithChild(i, newEntry, c, subChange);
                }
            }

            throw new Exception("Node with given relative path was not found");
        }

        void Update(Func<EditorState, Tuple<EditorState, JObject>> change)
        {
            Tuple<EditorState, JObject> newState;
            while (true)
            {
                var original = Volatile.Read(ref _state);
                newState = change(original);
                if (ReferenceEquals(Interlocked.CompareExchange(ref _state, newState.Item1, original), original))
                    break;
            }

            InitializeProjects(newState.Item1);
            if (newState.Item2 != null)
                Send(Messages.StateUpdate(newState.Item2));
        }

        [Command("dialog::cancel")]
        public void CancelDialog()
        {
            Update(Utils.Update((EditorState s, JObject c) => s.WithDialog(null, c)));
        }

        [Command("dialog:browse-project::open")]
        public void BrowseProject()
        {
            Update(Utils.Update((EditorState s, JObject c) => s.WithDialog(Dialog.Browse(), c)));
        }

        [Command("dialog:browse-project::open")]
        public void BrowseProject(string path)
        {
            Update(Utils.Update((EditorState s, JObject c) => s.WithDialog(Dialog.Browse(path), c)));
        }

        [Command("dialog:browse-project::select")]
        public void OpenProject(string path)
        {
            Update(Utils.Update((EditorState s, JObject c) =>
                s.WithDialog(null, c).WithWorkspace(Workspace.Create(path), c)
            ));

            if (_host != null)
                _host.Dispose();

            _host = new Host(path);
            _host.Connected += HostConnected;
            _host.References += HostReferences;
            _host.Start(Kvm.GetRuntime("default"));
        }

        [Command("tree:node::toggle")]
        public void TogleNode(string rel)
        {
            UpdateNode<Entry>(rel, (e, c) => e.AsOpen(!e.IsOpen, c));
        }

        private void HostReferences(object sender, ReferencesEventArgs e)
        {
            var proj = _state.Workspace.Projects.SingleOrDefault(p => p.Id == e.ContextId);
            if (proj == null)
                return;

            UpdateNode<ProjectEntry>(proj.RelativePath, (p, c) => p.WithReferences(e, c));
        }

        private void HostConnected(object sender, EventArgs e)
        {
            foreach (var proj in _state.Workspace.Projects)
                if (proj.Id > -1)
                    _host.InitProject(proj.Id, proj.Path);
        }
    }
}
