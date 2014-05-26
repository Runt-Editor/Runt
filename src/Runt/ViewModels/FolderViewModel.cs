using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class FolderViewModel : FileTreeViewModel
    {
        readonly DirectoryInfo _dir;
        readonly List<FolderViewModel> _dirs;
        readonly List<FileViewModel> _files;
        readonly FolderViewModel _parent;

        bool _initialized = false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public static FolderViewModel Get(FolderViewModel parent, DirectoryInfo dir)
        {
            if (dir.Name == "packages" && parent is WorkspaceViewModel)
                return new PackagesViewModel(parent, dir, true);

            if (dir.GetFiles("project.json", SearchOption.TopDirectoryOnly).Length == 1)
                return new ProjectViewModel(parent, dir);

            return new FolderViewModel(parent, dir);
        }

        protected override bool HasItems
        {
            get { return true; }
        }

        protected override IEnumerable GetItems()
        {
            if (!_initialized)
            {
                lock (this)
                {
                    if (!_initialized)
                    {
                        _dirs.AddRange(_dir.EnumerateDirectories().RemoveHidden().Select(d => Get(this, d)));
                        _files.AddRange(_dir.EnumerateFiles().RemoveHidden().Select(f => FileViewModel.Get(this, f)));
                        _initialized = true;
                    }
                }
            }

            return _dirs.Cast<object>().Concat(_files);
        }

        public FolderViewModel(FolderViewModel parent, string path)
            : this(parent, new DirectoryInfo(path))
        { }

        public FolderViewModel(FolderViewModel parent, DirectoryInfo dir)
            : base(parent)
        {
            _dir = dir;
            _parent = parent;

            _dirs = new List<FolderViewModel>();
            _files = new List<FileViewModel>();
        }

        public override string Name
        {
            get { return _dir.Name; }
        }

        public virtual string RelativePath
        {
            get { return Path.Combine(_parent.RelativePath, Name); }
        }

        public override void NotifyOfPropertyChange(string propertyName)
        {
            base.NotifyOfPropertyChange(propertyName);
            if (propertyName == "IsExpanded")
                NotifyOfPropertyChange(() => Icon);
        }

        public override ImageSource Icon
        {
            get
            {
                return IsExpanded ? Icons.FolderOpen : Icons.FolderNormal;
            }
        }
    }
}
