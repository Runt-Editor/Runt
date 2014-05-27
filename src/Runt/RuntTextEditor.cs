using System.IO;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Runt.Core;
using Runt.Services;

namespace Runt
{
    public class RuntTextEditor : TextEditor
    {
        readonly ILanguageService _lang;

        public RuntTextEditor(ILanguageService lang, string file)
        {
            _lang = lang;

            FontSize = 15;
            Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            FontFamily = new FontFamily("Consolas");
            ShowLineNumbers = true;
            Document = new TextDocument(new StringTextSource(File.ReadAllText(file)));

            SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
            //TextArea.TextView.ElementGenerators.Add(new LanguageElementGenerator(lang, file));
            //TextArea.TextView.LineTransformers.Add(new LanguageVisualLineTransformer(lang, file));
        }
    }
}
