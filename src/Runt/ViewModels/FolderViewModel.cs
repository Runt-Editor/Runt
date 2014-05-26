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

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public static FolderViewModel Get(FolderViewModel parent, DirectoryInfo dir)
        {
            FolderViewModel ret;
            if (dir.Name == "packages" && parent is WorkspaceViewModel)
                ret = new PackagesViewModel(parent, dir, true);

            else if (dir.GetFiles("project.json", SearchOption.TopDirectoryOnly).Length == 1)
                ret = new ProjectViewModel(parent, dir);

            else
                ret = new FolderViewModel(parent, dir);

            ret.Initialize();
            return ret;
        }

        protected override void Initialize()
        {
            _dirs.AddRange(_dir.EnumerateDirectories().RemoveHidden().Select(d => Get(this, d)));
            _files.AddRange(_dir.EnumerateFiles().RemoveHidden().Select(f => FileViewModel.Get(this, f)));
        }

        protected override bool HasItems
        {
            get { return true; }
        }

        protected override IEnumerable GetItems()
        {
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
            get { return System.IO.Path.Combine(_parent.RelativePath, Name); }
        }

        public virtual string Path
        {
            get { return _dir.FullName; }
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
