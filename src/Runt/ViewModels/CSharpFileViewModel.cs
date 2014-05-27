using System.IO;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.Text;
using Runt.Services;

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

        public override void DoubleClicked()
        {
            Workspace.OpenFile(_file, new CSharpLanguageService());
        }

        public FileInfo File
        {
            get { return _file; }
        }
    }
}
