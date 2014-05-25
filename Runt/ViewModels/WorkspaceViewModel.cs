using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.CodeAnalysis;
using Runt.ViewModels;

namespace Runt
{
    public class WorkspaceViewModel : FolderViewModel, IDisposable
    {
        readonly FileSystemWatcher _watcher;
        readonly CustomWorkspace _workspace;

        public WorkspaceViewModel(string path)
            : base(null, path)
        {
            _watcher = new FileSystemWatcher(path);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName | NotifyFilters.LastAccess;
            _watcher.Filter = "*.*";
            _watcher.Changed += FileChanged;

            _workspace = new CustomWorkspace();
        }

        internal ProjectId Add(string name)
        {
            return _workspace.AddProject(name, "C#");
        }

        internal Project this[ProjectId projectId]
        {
            get
            {
                return _workspace.CurrentSolution.GetProject(projectId);
            }
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
        }

        public static WorkspaceViewModel Load(string path)
        {
            Contract.Requires(path != null);

            if (!Directory.Exists(path))
                throw new ArgumentException("Directory does not exist");

            return new WorkspaceViewModel(path);
        }
    }
}
