using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Runt.DesignTimeHost;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class ProjectViewModel : FolderViewModel
    {
        readonly int _id;
        readonly Project _project;

        string _name;

        public ProjectViewModel(FolderViewModel parent, DirectoryInfo dir)
            : base(parent, dir)
        {
            var tuple = Workspace.Add(this);
            _id = tuple.Item1;
            _project = tuple.Item2;
        }

        internal void ApplyConfigurations(ConfigurationsEventArgs e)
        {
            _name = e.ProjectName;
        }

        public override ProjectViewModel Project
        {
            get { return this; }
        }

        public int Id
        {
            get { return _id; }
        }

        public override string Name
        {
            get { return _name ?? base.Name; }
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
