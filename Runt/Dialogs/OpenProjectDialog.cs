using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;

namespace Runt.Dialogs
{
    class OpenProjectDialog : IDisposable
    {
        readonly CommonOpenFileDialog _dialog;

        public OpenProjectDialog()
        {
            _dialog = new CommonOpenFileDialog("Open Project");
            _dialog.IsFolderPicker = true;
            _dialog.EnsurePathExists = true;
        }

        internal bool PickProject()
        {
            var result = _dialog.ShowDialog();
            switch (result)
            {
                case CommonFileDialogResult.Ok:
                    return true;

                default:
                    return false;
            }
        }

        public string Directory
        {
            get { return _dialog.FileName; }
        }

        public void Dispose()
        {
            _dialog.Dispose();
        }
    }
}
