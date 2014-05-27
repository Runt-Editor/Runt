using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Rendering;
using Runt.Core;

namespace Runt.Services
{
    class LanguageElementGenerator : VisualLineElementGenerator
    {
        private string file;
        private ILanguageService lang;

        public LanguageElementGenerator(ILanguageService lang, string file)
        {
            this.lang = lang;
            this.file = file;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            throw new NotImplementedException();
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var code = CurrentContext.GetText(0, CurrentContext.Document.TextLength);
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code.Text);
            
            throw new NotImplementedException();
        }
    }
}
