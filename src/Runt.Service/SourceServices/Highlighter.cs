using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using Runt.Service.CompilationModel;

namespace Runt.Service.SourceServices
{
    class Highlighter
    {
        readonly ProjectCompilation _compilation;

        public Highlighter(ProjectCompilation compilation)
        {
            _compilation = compilation;
        }

        internal JObject Highlight(string path)
        {
            SemanticModel model;
            SyntaxNode root;
            _compilation.GetRootForFile(path, out root, out model);
            return Highlight(root, model);
        }

        private JObject Highlight(SyntaxNode root, SemanticModel model)
        {
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
                var c = string.Join(" ", g.Where(r => !string.IsNullOrEmpty(r.Style)).Select(r => r.Style).Distinct());
                var props = new JObject(g.Where(r => r.Props != null).SelectMany(r => r.Props.Properties()));
                if (props.Count == 0)
                    props = null;

                return new Visitor.Region
                {
                    Start = k.Start,
                    Line = k.Line,
                    End = k.End,
                    Style = c,
                    Props = props
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

                var annotation = JObject.FromObject(new AnnotationRange(region.Start, region.End, new OrionStyle(styleClass: region.Style, attributes: region.Props)));
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
            if (prop == null)
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

        class Visitor : CSharpSyntaxWalker
        {
            public class Region
            {
                public int Line { get; set; }
                public int Start { get; set; }
                public int End { get; set; }
                public string Style { get; set; }
                public JObject Props { get; set; }
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

            void Mark(SyntaxToken token, string type = null, JObject props = null)
            {
                var location = token.GetLocation().GetLineSpan();
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
                        Style = type,
                        Props = props
                    });
                }
            }

            void Mark(SyntaxTrivia trivia, string type, JObject props = null)
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
                        Style = type,
                        Props = props
                    });
                }
            }

            TokenType GetTokenType(SyntaxToken token)
            {
                var declarationSymbol = _model.GetDeclaredSymbol(token.Parent);
                if (declarationSymbol == null)
                {
                    // The token isnt part of a declaration node, so try to get symbol info.
                    var referenceSymbol = _model.GetSymbolInfo(token.Parent).Symbol;
                    if (referenceSymbol == null)
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

            static string LocStr(Location loc)
            {
                return string.Concat(loc.SourceSpan.Start, ",", loc.SourceSpan.End);
            }

            static Regex capital = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
            void VisitSymbol(SyntaxToken token, ISymbol symbol)
            {
                JObject data = new JObject(
                    new JProperty("data-symbol", new JValue(LocStr(token.GetLocation())))
                );

                var parts = symbol.ToDisplayParts(new SymbolDisplayFormat());
                var part = parts.SingleOrDefault(p => p.Symbol == symbol);
                if (part.Symbol != null)
                {
                    string className = part.Kind.ToString();
                    className = capital.Replace(className, "$1-$2").ToLower();

                    Mark(token, className, data);
                }

                //SymbolDisplayPartKind kind = SymbolDisplayPartKind.Text;
                //if (symbol is IAssemblySymbol)
                //    kind = SymbolDisplayPartKind.AssemblyName;
                //else if (symbol is IModuleSymbol)
                //    kind = SymbolDisplayPartKind.ModuleName;
                //else if (symbol is INamespaceSymbol)
                //    kind = ((INamespaceSymbol)symbol).IsGlobalNamespace ? SymbolDisplayPartKind.Keyword : SymbolDisplayPartKind.NamespaceName;
                //else if (symbol is ILocalSymbol)
                //    kind = SymbolDisplayPartKind.LocalName;
                //else if (symbol is IRangeVariableSymbol)
                //    kind = SymbolDisplayPartKind.RangeVariableName;
                //else if (symbol is ILabelSymbol)
                //    kind = SymbolDisplayPartKind.LabelName;
                //else if (symbol is IAliasSymbol)
                //    kind = SymbolDisplayPartKind.AliasName;
                //else if (symbol is INamedTypeSymbol)
                //    kind = SymbolDisplayPartKind.ClassName;

            }

            void ClassifyIdentifierToken(SyntaxToken token)
            {
                var tokenType = GetTokenType(token);
                switch (tokenType.Type)
                {
                    case TokenType.Kind.Reference:
                    case TokenType.Kind.Declaration:
                        VisitSymbol(token, tokenType.Symbol);
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
                if (diagnostics != null)
                {
                    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                        Mark(token, "error");
                    else
                        Mark(token, "warning");
                }

                if (token.IsKeyword() || token.IsContextualKeyword())
                {
                    Mark(token, "keyword");
                    var type = GetTokenType(token);
                    if (type.Type != TokenType.Kind.Unknown)
                        ClassifyIdentifierToken(token);
                }

                var kind = token.CSharpContextualKind();
                switch (kind)
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

                switch (trivia.CSharpKind())
                {
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
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
