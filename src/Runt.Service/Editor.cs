﻿using System;
using System.Threading;
using Newtonsoft.Json;
using Runt.Core;
using Runt.Core.Model;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using Runt.DesignTimeHost;
using Runt.Core.Model.FileTree;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text;
using Runt.Service.Highlighting;

namespace Runt.Service
{
    public class Editor : IEditor
    {
        private ConcurrentDictionary<string, Content> _contentDictionary = new ConcurrentDictionary<string, Content>();
        private EditorState _state = EditorState.Null;
        private FileSystemWatcher _watcher;
        private Host _host;
        private int _nextId;
        private int _nextUpdate = 0;

        public IClientConnection ClientConnection { get; set; }

        public void NotifyConnected()
        {
            _nextUpdate = 0;
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
            // TODO: Cache dictionary of command-name -> Action
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

        void UpdateNode<TNodeType>(string relPath, Func<TNodeType, JObject, TNodeType> update, Func<EditorState, JObject, EditorState> postChanges = null)
            where TNodeType : Entry
        {
            UpdateNodes(new[] { new Tuple<string, Func<TNodeType, JObject, TNodeType>>(relPath, update) }, postChanges);
        }

        void UpdateNodes<TNodeType>(IEnumerable<Tuple<string, Func<TNodeType, JObject, TNodeType>>> updates, Func<EditorState, JObject, EditorState> postChanges = null)
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
                var newState = s.WithWorkspace(workspace, c, workspaceC);
                if (postChanges != null)
                    return postChanges(newState, c);
                return newState;
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

                    if (!newEntry.IsOpen && subChange.Property("children") != null)
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

        Content GetContent(string contentId, bool read)
        {
            Content result;
            if (!read)
            {
                if (_contentDictionary.TryGetValue(contentId, out result))
                    return result;
            }

            var parts = contentId.Split(new[] { ':' }, 2);
            var type = parts[0];
            var name = parts[1];

            Func<Content> get = () =>
            {
                switch (type)
                {
                    case "edit":
                        return FileContent.Create(contentId, name, Path.Combine(_state.Workspace.Path, name), read);

                    default:
                        throw new ArgumentException("Unknonwn content-type: " + type + "");
                }
            };

            if (!read)
                return get();
            else
                return _contentDictionary.GetOrAdd(contentId, s => get());
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
                s.Reset(c).WithWorkspace(Workspace.Create(path), c)
            ));

            if (_host != null)
                _host.Dispose();

            _host = new Host(path);
            _host.Connected += HostConnected;
            _host.References += HostReferences;
            _host.Sources += HostSources;
            _host.Diagnostics += HostDiagnostics;
            _host.Start(Kvm.GetRuntime("default"));
        }


        [Command("tree:node::toggle")]
        public void TogleNode(string rel)
        {
            UpdateNode<Entry>(rel, (e, c) => e.AsOpen(!e.IsOpen, c));
        }

        [Command("tab::open")]
        public void OpenTab(string contentId)
        {
            Update(Utils.Update((EditorState s, JObject c) =>
            {
                JObject tabChanges = new JObject();
                JObject partial;
                Tab newVal;
                bool create = true;
                var tabs = s.Tabs.ToBuilder();
                for (int i = 0, l = tabs.Count; i < l; i++)
                {
                    partial = new JObject();
                    if (tabs[i].ContentId == contentId)
                    {
                        create = false;
                        newVal = tabs[i].AsActive(true, partial);
                    }
                    else
                    {
                        newVal = tabs[i].AsActive(false, partial);
                    }
                    Core.Utils.RegisterChange(tabChanges, i.ToString(), newVal, partial);
                    tabs[i] = newVal;
                }

                if (create)
                {
                    var content = GetContent(contentId, false);
                    newVal = new Tab(contentId, content.Name, content.Tooltip, false, true);
                    Core.Utils.RegisterChange(tabChanges, tabs.Count.ToString(), newVal, null);
                    tabs.Add(newVal);
                }

                var t = tabs.ToImmutable();
                return s.WithTabs(t, c, tabChanges);
            }));
        }

        [Command("tab::select")]
        public void SelectTab(string contentId)
        {
            Update(Utils.Update((EditorState s, JObject c) =>
            {
                JObject tabChanges = new JObject();
                JObject partial;
                Tab newVal;
                var tabs = s.Tabs.ToBuilder();
                for (int i = 0, l = tabs.Count; i < l; i++)
                {
                    partial = new JObject();
                    newVal = tabs[i].AsActive(tabs[i].ContentId == contentId, partial);
                    Core.Utils.RegisterChange(tabChanges, i.ToString(), newVal, partial);
                    tabs[i] = newVal;
                }

                var t = tabs.ToImmutable();
                return s.WithTabs(t, c, tabChanges);
            }));
        }

        [Command("tab::close")]
        public void CloseTab(string contentId)
        {
            // TODO: if active, automatically select another tab
            Update(Utils.Update((EditorState s, JObject c) =>
            {
                var tabs = s.Tabs.ToBuilder();
                for (int i = 0, l = tabs.Count; i < l; i++)
                {
                    if (tabs[i].ContentId == contentId)
                    {
                        if (tabs[i].Dirty)
                            throw new NotImplementedException("Notifying of dirty tabs beeing closed is not yet implemented");

                        tabs.RemoveAt(i);
                        break;
                    }
                }

                var t = tabs.ToImmutable();
                return s.WithTabs(t, c, null);
            }));
        }

        // reason for default = noop, later plan to add more options
        public void MarkTab(string contentId, bool? dirty = null)
        {
            if (!dirty.HasValue)
                return;

            Update(Utils.Update((EditorState s, JObject c) =>
            {
                var tabs = s.Tabs;
                int index = -1;
                Tab tab = null;
                for (int i = 0, l = tabs.Count; i < l; i++)
                {
                    tab = tabs[i];
                    if(tab.ContentId == contentId)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                    throw new Exception("tab not found");

                JObject tabChange = new JObject();
                JObject tabListChanges = new JObject();
                if (dirty.HasValue)
                    tab = tab.AsDirty(dirty.Value, tabChange);

                Core.Utils.RegisterChange(tabListChanges, index.ToString(), tab, tabChange);
                return s.WithTabs(tabs.SetItem(index, tab), c, tabListChanges);
            }));
        }

        [Command("content::load")]
        public void LoadContent(string contentId)
        {
            var content = GetContent(contentId, true);
            Send(Messages.Content(content));
        }

        [Command("code::update")]
        public void UpdateCode(string contentId, TextDiff update)
        {
            Task.Run(() =>
            {
                // updates needs to be processed in the same order they are generated
                // this method might wait for the next update, thus running in background
                // thread
                Content content;
                var dict = _contentDictionary;
                lock (dict)
                {
                    while (update.Update != Volatile.Read(ref _nextUpdate))
                        Monitor.Wait(dict);

                    Interlocked.Increment(ref _nextUpdate);

                    if (!_contentDictionary.TryGetValue(contentId, out content))
                        throw new Exception("Content wasn't in dictionary. Illegal code::update sent");

                    if (update.Start == 0 && update.Added == content.ContentString.Length && update.Text == content.ContentString)
                        goto highlight; // editor sends updates when files are opened

                    var text = content.ContentString;
                    var sb = new StringBuilder().Append(text, 0, update.Start);
                    sb.Append(update.Text);
                    sb.Append(text, update.Start + update.Removed, text.Length - update.Start - update.Removed);
                    text = sb.ToString();
                    Content old = content;
                    content = content.WithText(text);

                    if (!_contentDictionary.TryUpdate(contentId, content, old))
                        throw new Exception("Content has been updated by other method. State inconclusive.");
                }

                // mark dirty
                MarkTab(contentId, dirty: true);

                highlight:
                Highlight(content);
            });
        }

        private void Highlight(Content content)
        {
            var workspace = _state.Workspace;
            if (workspace == null)
                return;

            var proj = workspace.Projects.SingleOrDefault(p => content.RelativePath.StartsWith(p.RelativePath, StringComparison.OrdinalIgnoreCase));
            if (proj == null)
                return;

            if (proj.Sources.Count == 0)
                return;

            if (proj.References == null)
                return;

            var highlight = new Highlighter(proj).Highlight(_contentDictionary.Values.ToDictionary(c => Path.Combine(workspace.Path, c.RelativePath)), content);

            Send(Messages.Highlight(content.ContentId, highlight));

        }

        private void HostDiagnostics(object sender, DiagnosticsEventArgs e)
        {
            var proj = _state.Workspace.Projects.SingleOrDefault(p => p.Id == e.ContextId);
            if (proj == null)
                return;

            UpdateNode<ProjectEntry>(proj.RelativePath, (p, c) => p.WithDiagnostics(e.Errors, e.Warnings, c), (s, c) =>
            {
                // Workspace will be "updated" at this point, even though the update is about to be culled away, as it is empty
                Core.Utils.RegisterChange((JObject)c.Property(Core.Utils.NameOf(() => s.Workspace)).Value, () => s.Workspace.ErrorList, s.Workspace.ErrorList, null);
                return s;
            });
        }

        private void HostSources(object sender, SourcesEventArgs e)
        {
            var proj = _state.Workspace.Projects.SingleOrDefault(p => p.Id == e.ContextId);
            if (proj == null)
                return;

            UpdateNode<ProjectEntry>(proj.RelativePath, (p, c) => p.WithSources(e));
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
