using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.CodeAnalysis;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class ProjectViewModel : FolderViewModel
    {
        readonly ProjectId _id;

        public ProjectViewModel(FolderViewModel parent, DirectoryInfo dir)
            : base(parent, dir)
        {
            _id = Workspace.Add(Name);
        }

        public override ProjectViewModel Project
        {
            get { return this; }
        }

        public ProjectId Id
        {
            get { return _id; }
        }

        internal DocumentId Add(CSharpFileViewModel fileViewModel)
        {
            var doc = Workspace[_id].AddDocument(fileViewModel.Name, fileViewModel.ReadContent());
            return doc.Id;
        }

        public override ImageSource Icon
        {
            get
            {
                return Icons.Project;
            }
        }
    }
}
