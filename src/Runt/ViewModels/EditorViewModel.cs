using System.IO;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit;

namespace Runt.ViewModels
{
    public class EditorViewModel : Screen, IEditorScreenViewModel
    {
        readonly FileInfo _file;

        TextEditor _editor;

        string _content;

        public EditorViewModel(FileInfo file)
        {
            DisplayName = file.Name;
            _file = file;
        }

        private void Setup()
        {
            _editor.Text = File.ReadAllText(_file.FullName);
        }

        protected override void OnViewLoaded(object view)
        {
            _editor = (TextEditor)view;
            Setup();
            base.OnViewLoaded(view);
        }
    }
}
