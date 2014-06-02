using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using Runt.Core;
using Runt.Core.Model;
using Runt.Core.Model.FileTree;

namespace Runt.Service.Highlighting
{
    public class Highlighter
    {
        readonly ProjectEntry _proj;

        public Highlighter(ProjectEntry proj)
        {
            _proj = proj;
        }

        internal JObject Highlight(Dictionary<string, Content> contentLookup, Content content)
        {
            var references = _proj.References.Select(GetReference).ToArray();
            var sources = new Dictionary<string, SyntaxTree>();
            SyntaxTree wanted = null;
            foreach (var f in _proj.Sources)
                sources.Add(f, Lookup(contentLookup, f, content, ref wanted));


            if (wanted == null)
                return null;

            CSharpCompilation compilation = CSharpCompilation.Create(_proj.Name, references: references, syntaxTrees: sources.Values);
            var semantic = compilation.GetSemanticModel(wanted);
            var root = wanted.GetRoot();
            return Highlight(root, semantic, content.ContentString);
        }

        private JObject Highlight(SyntaxNode root, SemanticModel model, string text)
        {
            Func<int, int> lineOffset = glob => glob - text.LastIndexOfAny(new[] { '\r', '\n' }, 0, glob);
            
            var visitor = new Visitor(model);
            visitor.Visit(root);
            var regions = visitor.Regions.OrderBy(r => r.Line).ThenBy(r => r.Start)
                .GroupBy(r => new
            {
                Line = r.Line,
                Start = r.Start,
                End = r.End
            }).Select(g =>
            {
                var k = g.Key;
                var c = string.Join(" ", g.Select(r => r.Style).Distinct());

                return new Visitor.Region
                {
                    Start = k.Start,
                    Line = k.Line,
                    End = k.End,
                    Style = c
                };
            });

            JObject lines = new JObject();
            foreach (var region in regions)
            {
                JArray lregions;
                var lineProp = lines.Property(region.Line.ToString());
                if (lineProp == null)
                {
                    lineProp = new JProperty(region.Line.ToString());
                    lregions = new JArray();
                    lineProp.Value = new JObject(new JProperty("ranges", lregions));
                    lines.Add(lineProp);
                }
                else
                {
                    lregions = (JArray)((JObject)lineProp.Value).Property("ranges").Value;
                }

                var annotation = JObject.FromObject(new AnnotationRange(region.Start, region.End, new OrionStyle(styleClass: region.Style)));
                if (region.Style.Contains("error"))
                    AddError((JObject)lineProp.Value, annotation);
                lregions.Add(annotation);
            }

            return lines;
        }

        void AddError(JObject line, JObject error)
        {
            JArray errors;
            var prop = line.Property("errors");
            if(prop == null)
            {
                prop = new JProperty("errors", errors = new JArray());
                line.Add(prop);
            }
            else
            {
                errors = (JArray)prop.Value;
            }

            errors.Add(error);
        }

        private SyntaxTree Lookup(Dictionary<string, Content> contentLookup, string path, Content highlighting, ref SyntaxTree wanted)
        {
            Content content = null;
            string text;
            if (contentLookup.TryGetValue(path, out content))
                text = content.ContentString;
            else
                text = File.ReadAllText(path);

            if(content.RelativePath == highlighting.RelativePath)
            {
                text = highlighting.ContentString;
                wanted = CSharpSyntaxTree.ParseText(text, path: path);
                return wanted;
            }

            return CSharpSyntaxTree.ParseText(text, path: path);
        }

        private static MetadataReference GetReference(ReferencesEventArgs.Package package)
        {
            switch (package.Type)
            {
                case "Package":
                case "Assembly":
                    return new MetadataFileReference(package.Path);

                case "Project":
                    throw new NotImplementedException();

                default:
                    throw new Exception("Unknown package type");
            }
        }

        class Visitor : CSharpSyntaxWalker
        {
            public class Region
            {
                public int Line { get; set; }
                public int Start { get; set; }
                public int End { get; set; }
                public string Style { get; set; }
            }

            readonly SemanticModel _model;
            readonly ImmutableArray<Diagnostic> _modelDiagnostic;
            List<Region> _regions = new List<Region>();

            public ImmutableList<Region> Regions
            {
                get { return _regions.ToImmutableList(); }
            }

            public Visitor(SemanticModel model)
                : base(SyntaxWalkerDepth.Trivia)
            {
                _model = model;
                _modelDiagnostic = _model.GetDiagnostics();
            }

            ImmutableList<Diagnostic> Diagnostics(SyntaxToken token)
            {
                var diagnostics = _modelDiagnostic.Where(d => d.Location.SourceSpan.Contains(token.Span))
                    .Where(d => d.Severity == DiagnosticSeverity.Warning || d.Severity == DiagnosticSeverity.Error)
                    .ToImmutableList();
                if (diagnostics.Count == 0)
                    return null;
                return diagnostics;
            }

            void Mark(SyntaxToken token, string type)
            {
                var location = token.GetLocation().GetLineSpan();
                var startLine = location.StartLinePosition.Line;
                var endLine = location.EndLinePosition.Line;

                for(var i = startLine; i <= endLine; i++)
                {
                    var start = i == startLine ? location.StartLinePosition.Character : 0;
                    var end = i == endLine ? location.EndLinePosition.Character : int.MaxValue;

                    _regions.Add(new Region
                    {
                        Line = i,
                        Start = start,
                        End = end,
                        Style = type
                    });
                }
            }

            void Mark(SyntaxTrivia trivia, string type)
            {
                var location = trivia.GetLocation().GetLineSpan();
                var startLine = location.StartLinePosition.Line;
                var endLine = location.EndLinePosition.Line;

                for (var i = startLine; i <= endLine; i++)
                {
                    var start = i == startLine ? location.StartLinePosition.Character : 0;
                    var end = i == endLine ? location.EndLinePosition.Character : int.MaxValue;

                    _regions.Add(new Region
                    {
                        Line = i,
                        Start = start,
                        End = end,
                        Style = type
                    });
                }
            }

            TokenType GetTokenType(SyntaxToken token)
            {
                var declarationSymbol = _model.GetDeclaredSymbol(token.Parent);
                if(declarationSymbol == null)
                {
                    // The token isnt part of a declaration node, so try to get symbol info.
                    var referenceSymbol = _model.GetSymbolInfo(token.Parent).Symbol;
                    if(referenceSymbol == null)
                    {
                        // we couldnt find symbol information for the node, so we will look at all symbols in scope by name.
                        var namedSymbols = _model.LookupSymbols(token.SpanStart, null, token.ToString(), true);
                        if (namedSymbols.Length == 1)
                            return TokenType.Reference(namedSymbols[0]);

                        return TokenType.Unknown();
                    }

                    return TokenType.Reference(referenceSymbol);
                }

                // The token is part of a declaration syntax node.
                return TokenType.Declaration(declarationSymbol);
            }

            void VisitReference(SyntaxToken token, ISymbol symbol)
            {
                Mark(token, "reference");
                if (symbol is IFieldSymbol)
                {
                    var isEnum = symbol.ContainingType != null && symbol.ContainingType.EnumUnderlyingType != null;
                    if (isEnum)
                        Mark(token, "enum-field");
                    else
                        Mark(token, "field");
                }
                else if (symbol is ILocalSymbol)
                    Mark(token, "local-variable");
                else if (symbol is IParameterSymbol)
                    Mark(token, "parameter");
                else if (symbol is IPropertySymbol)
                    Mark(token, "property");
                else if (symbol is INamedTypeSymbol)
                    Mark(token, "named-type");
                else if (symbol is IMethodSymbol)
                    Mark(token, "method");
                else if (symbol is INamespaceSymbol)
                    Mark(token, "namespace");
                else
                    Mark(token, "identifier");
            }

            void VisitDeclaration(SyntaxToken token, ISymbol symbol)
            {
                Mark(token, "declaration");
                if (symbol is IFieldSymbol)
                {
                    var isEnum = symbol.ContainingType != null && symbol.ContainingType.EnumUnderlyingType != null;
                    if (isEnum)
                        Mark(token, "enum-field");
                    else
                        Mark(token, "field");
                }
                else if (symbol is ILocalSymbol)
                    Mark(token, "local-variable");
                else if (symbol is INamedTypeSymbol)
                    Mark(token, "named-type");
                else if (symbol is IPropertySymbol)
                    Mark(token, "property");
                else if (symbol is IMethodSymbol)
                    Mark(token, "method");
                else if (symbol is IParameterSymbol)
                    Mark(token, "parameter");
                else
                    Mark(token, "identifier");
            }

            void ClassifyIdentifierToken(SyntaxToken token)
            {
                var tokenType = GetTokenType(token);
                switch (tokenType.Type)
                {
                    case TokenType.Kind.Reference:
                        VisitReference(token, tokenType.Symbol);
                        break;

                    case TokenType.Kind.Declaration:
                        VisitDeclaration(token, tokenType.Symbol);
                        break;

                    default:
                        Mark(token, "identifier");
                        break;
                }
            }

            public override void VisitToken(SyntaxToken token)
            {
                VisitLeadingTrivia(token);

                var diagnostics = Diagnostics(token);
                if(diagnostics != null)
                {
                    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                        Mark(token, "error");
                    else
                        Mark(token, "warning");
                }

                if (token.IsKeyword() || token.IsContextualKeyword())
                    Mark(token, "keyword");

                var kind = token.CSharpContextualKind();
                switch(kind)
                {
                    case SyntaxKind.IdentifierToken:
                        ClassifyIdentifierToken(token); break;

                    case SyntaxKind.StringLiteralToken:
                        Mark(token, "string"); break;

                    case SyntaxKind.NumericLiteralToken:
                        Mark(token, "number"); break;

                    case SyntaxKind.CharacterLiteralToken:
                        Mark(token, "char"); break;
                }

                VisitTrailingTrivia(token);
            }

            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.HasStructure)
                    Visit(trivia.GetStructure());
                else
                {
                    switch(trivia.CSharpKind())
                    {
                        case SyntaxKind.MultiLineCommentTrivia:
                        case SyntaxKind.SingleLineCommentTrivia:
                            Mark(trivia, "comment"); break;

                        case SyntaxKind.EndOfLineTrivia:
                        case SyntaxKind.WhitespaceTrivia:
                            Mark(trivia, "whitespace"); break;

                        case SyntaxKind.RegionDirectiveTrivia:
                            Mark(trivia, "begin-region"); break;

                        case SyntaxKind.EndRegionDirectiveTrivia:
                            Mark(trivia, "end-region"); break;

                        case SyntaxKind.DisabledTextTrivia:
                            Mark(trivia, "disabled-text"); break;
                    }
                }
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                base.VisitClassDeclaration(node);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                base.VisitEnumDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                base.VisitStructDeclaration(node);
            }
        }
    }
}
