using System;
using System.Collections;
using System.IO;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class FileViewModel : FileTreeViewModel
    {
        protected readonly FileInfo _file;
        protected readonly FolderViewModel _parent;

        public FileViewModel(FolderViewModel parent, string path)
            : this(parent, new FileInfo(path))
        { }

        public FileViewModel(FolderViewModel parent, FileInfo file)
            : base(parent)
        {
            _parent = parent;
            _file = file;
        }

        protected override bool HasItems
        {
            get { return false; }
        }

        protected override IEnumerable GetItems()
        {
            throw new NotSupportedException();
        }

        public override string Name
        {
            get { return _file.Name; }
        }

        public override ImageSource Icon
        {
            get
            {
                return Icons.Document;
            }
        }

        internal static FileViewModel Get(FolderViewModel folderViewModel, FileInfo file)
        {
            if (file.Extension == ".cs")
                return new CSharpFileViewModel(folderViewModel, file);
            return new FileViewModel(folderViewModel, file);
        }
    }
}
