using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class PackagesViewModel : FolderViewModel
    {
        bool _isRoot;

        public PackagesViewModel(FolderViewModel parent, DirectoryInfo dir, bool isRoot)
            : base(parent, dir)
        {
            _isRoot = isRoot;
        }

        protected override IEnumerable GetItems()
        {
            return Enumerable.Empty<object>();
        }

        public override ImageSource Icon
        {
            get { return Icons.NuGet; }
        }
    }
}
