using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class CSharpFileViewModel : FileViewModel
    {
        public CSharpFileViewModel(FolderViewModel parent, FileInfo file)
            : base(parent, file)
        {
        }

        internal SourceText ReadContent()
        {
            using (var stream = _file.OpenRead())
                return SourceText.From(stream);
        }

        public override ImageSource Icon
        {
            get { return Icons.CSFile; }
        }
    }
}
