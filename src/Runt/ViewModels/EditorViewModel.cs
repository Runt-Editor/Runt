using System.IO;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Runt.Core;

namespace Runt.ViewModels
{
    public class EditorViewModel : Screen, IEditorScreenViewModel
    {
        readonly FileInfo _file;
        readonly ILanguageService _language;
        readonly TextDocument _document;

        TextEditor _editor;

        string _content;

        public EditorViewModel(FileInfo file, ILanguageService language)
        {
            DisplayName = file.Name;
            _file = file;
            _language = language;
        }

        private void Setup()
        {
            //_editor.Text = File.ReadAllText(_file.FullName);
        }

        protected override void OnViewLoaded(object view)
        {
            _editor = (RuntTextEditor)view;
            Setup();
            base.OnViewLoaded(view);
        }

        public ILanguageService Language
        {
            get { return _language; }
        }

        public string File
        {
            get { return _file.FullName; }
        }
    }
}
