using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Caliburn.Micro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using Runt.Dialogs;

namespace Runt.ViewModels
{
    public class ShellViewModel : Conductor<IEditorScreenViewModel>.Collection.OneActive, IDisposable
    {
        readonly IWindowManager _wm;
        readonly BindableCollection<string> _aliases;

        FileSystemWatcher _watcher;
        bool _installKvm;

        string _selectedRuntime;

        public string SelectedRuntime
        {
            get { return _selectedRuntime; }
            set
            {
                _selectedRuntime = value;
                NotifyOfPropertyChange(() => SelectedRuntime);
            }
        }

        public BindableCollection<string> Runtimes
        {
            get { return _aliases; }
        }

        public ShellViewModel(IWindowManager wm)
        {
            _wm = wm;
            DisplayName = "Runt";

            _aliases = new BindableCollection<string>(Runt.Kvm.ListAlias().Select(a => a.Alias));

            if (!Runt.Kvm.HasAlias("default"))
                _installKvm = true;
            else
            {
                _watcher = new FileSystemWatcher(Runt.Kvm.AliasDirectory);
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName | NotifyFilters.LastAccess;
                _watcher.Filter = "*.*";
                _watcher.Changed += AliasChanged;
                SelectedRuntime = "default";
            }
        }

        private void AliasChanged(object sender, FileSystemEventArgs e)
        {
            var currentAlias = SelectedRuntime;
            _aliases.Clear();
            _aliases.AddRange(Runt.Kvm.ListAlias().Select(a => a.Alias));
            SelectedRuntime = currentAlias;
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            if (_installKvm)
                Execute.OnUIThreadAsync(async () => await InstallKvm((MetroWindow)((UserControl)view).Parent));
        }

        private async Task InstallKvm(MetroWindow window)
        {
            var ctrl = await window.ShowProgressAsync("Installing KRE", "Please wait while KRE is beeing installed");
            ctrl.SetIndeterminate();
            await Runt.Kvm.Upgrade();
            await ctrl.CloseAsync();
            _installKvm = false;

            _watcher = new FileSystemWatcher(Runt.Kvm.AliasDirectory);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName | NotifyFilters.LastAccess;
            _watcher.Filter = "*.*";
            _watcher.Changed += AliasChanged;
            _aliases.Clear();
            _aliases.AddRange(Runt.Kvm.ListAlias().Select(a => a.Alias));
            SelectedRuntime = "default";
        }

        WorkspaceViewModel _project;

        public void Open()
        {
            using(var dialog = new OpenProjectDialog())
            {
                if(dialog.PickProject())
                {
                    var project = WorkspaceViewModel.Load(this, dialog.Directory);

                    _project = project;
                    NotifyOfPropertyChange(() => Project);
                }
            }
        }

        public void OpenFile()
        {
            Debugger.Break();
        }

        public void ManageKvmAlias()
        {
            _wm.ShowDialog(new Kvm.AliasManagerViewModel());
        }

        public void ManageKvmRuntimes()
        {
            _wm.ShowDialog(new Kvm.RuntimeManagerViewModel());
        }

        public void Dispose()
        {
            _project.Dispose();
            _watcher.Dispose();
        }

        public WorkspaceViewModel Project
        {
            get { return _project; }
        }
    }
}
