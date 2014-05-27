using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.CodeAnalysis;
using Runt.Core;
using Runt.DesignTimeHost;
using Runt.ViewModels;

namespace Runt
{
    public class WorkspaceViewModel : FolderViewModel, IDisposable
    {
        readonly ShellViewModel _shell;
        readonly FileSystemWatcher _watcher;
        readonly CustomWorkspace _workspace;
        readonly Host _host;
        readonly ConcurrentDictionary<int, ProjectViewModel> _contexts = new ConcurrentDictionary<int, ProjectViewModel>();

        int _projectsIds;
        TaskCompletionSource<bool> _connect;

        public WorkspaceViewModel(ShellViewModel shell, string path)
            : base(null, path)
        {
            _host = new Host(path);
            _host.Connected += HostConnected;
            _host.Configurations += HostConfigurations;
            _host.References += HostReferences;

            _watcher = new FileSystemWatcher(path);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName | NotifyFilters.LastAccess;
            _watcher.Filter = "*.*";
            _watcher.Changed += FileChanged;

            _workspace = new CustomWorkspace();
            _shell = shell;

            InitializeWorkspace();
        }

        private async void InitializeWorkspace()
        {
            var ctrl = await _shell.Window.ShowProgressAsync("Setting up workspace", "Restoring packages");
            ctrl.SetIndeterminate();

            var runtime = Kvm.GetRuntime(_shell.SelectedRuntime);
            await _host.RestorePackages(runtime, _dir.FullName);
            ctrl.SetMessage("Setting up projects");
            _connect = new TaskCompletionSource<bool>();
            _host.Start(runtime);
            await _connect.Task;
            _connect = null;
            await ctrl.CloseAsync();
        }

        internal void OpenFile(FileInfo file, ILanguageService language)
        {
            var item = new EditorViewModel(file, language);
            _shell.Items.Add(item);
            _shell.ActivateItem(item);
        }

        private void HostReferences(object sender, ReferencesEventArgs e)
        {
            var project = _contexts[e.ContextId];
            project.ApplyReferences(e);

            if (_connect != null)
                if (_contexts.Values.All(p => p.Configurated))
                    _connect.TrySetResult(true);
        }

        private void HostConfigurations(object sender, ConfigurationsEventArgs e)
        {
            var project = _contexts[e.ContextId];
            project.ApplyConfigurations(e);

            if (_connect != null)
                if (_contexts.Values.All(p => p.Configurated))
                    _connect.TrySetResult(true);
        }

        private void HostConnected(object sender, EventArgs e)
        {
            foreach (var proj in _contexts.Values)
                _host.InitProject(proj.Id, proj.Path);
        }

        internal Tuple<int, Project> Add(ProjectViewModel project)
        {
            var pid = _workspace.AddProject(project.Name, "C#");
            var proj = _workspace.CurrentSolution.GetProject(pid);
            int id = Interlocked.Increment(ref _projectsIds);

            if (!_contexts.TryAdd(id, project))
                throw new Exception("Id already taken");

            if (_host.IsConnected)
                _host.InitProject(id, project.Path);

            return new Tuple<int, Project>(id, proj);
        }

        public override WorkspaceViewModel Workspace
        {
            get { return this; }
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {

        }

        public override string RelativePath
        {
            get { return string.Empty; }
        }

        public void Dispose()
        {
            _watcher.Dispose();
            _host.Dispose();
            _workspace.Dispose();
        }

        public static WorkspaceViewModel Load(ShellViewModel shell, string path)
        {
            Contract.Requires(path != null);

            if (!Directory.Exists(path))
                throw new ArgumentException("Directory does not exist");

            var workspace = new WorkspaceViewModel(shell, path);
            workspace.Initialize();
            return workspace;
        }
    }
}
