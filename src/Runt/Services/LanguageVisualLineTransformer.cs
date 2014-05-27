using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Rendering;
using Runt.Core;

namespace Runt.Services
{
    internal class LanguageVisualLineTransformer : IVisualLineTransformer
    {
        readonly ILanguageService _lang;
        readonly string _path;

        public LanguageVisualLineTransformer(ILanguageService lang, string path)
        {
            _lang = lang;
            _path = path;
        }

        void IVisualLineTransformer.Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
        {
            ; // do nothing
        }
    }
}