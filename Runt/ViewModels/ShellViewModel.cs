using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.WindowsAPICodePack.Dialogs;
using Runt.Dialogs;

namespace Runt.ViewModels
{
    public class ShellViewModel : Conductor<IEditorScreenViewModel>.Collection.OneActive
    {
        public ShellViewModel()
        {
            DisplayName = "Runt";
        }

        WorkspaceViewModel _project;

        public void Open()
        {
            using(var dialog = new OpenProjectDialog())
            {
                if(dialog.PickProject())
                {
                    var project = WorkspaceViewModel.Load(dialog.Directory);

                    _project = project;
                    NotifyOfPropertyChange(() => Project);
                }
            }
        }

        public WorkspaceViewModel Project
        {
            get { return _project; }
        }
    }
}
