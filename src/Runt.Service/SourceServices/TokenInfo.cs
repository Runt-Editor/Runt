using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;

namespace Runt.Service.SourceServices
{
    class TokenInfo
    {
        const SymbolDisplayGlobalNamespaceStyle globalStyle = SymbolDisplayGlobalNamespaceStyle.Omitted;
        const SymbolDisplayTypeQualificationStyle typeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces;
        const SymbolDisplayGenericsOptions genericOptions = SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance;
        const SymbolDisplayMemberOptions memberOptions = SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeModifiers
            | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType;
        const SymbolDisplayExtensionMethodStyle extensionStyle = SymbolDisplayExtensionMethodStyle.InstanceMethod;
        const SymbolDisplayPropertyStyle propertyStyle = SymbolDisplayPropertyStyle.ShowReadWriteDescriptor;
        const SymbolDisplayLocalOptions localOptions = SymbolDisplayLocalOptions.IncludeType | SymbolDisplayLocalOptions.IncludeConstantValue;
        const SymbolDisplayParameterOptions paramOptions = SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeExtensionThis
            | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeOptionalBrackets | SymbolDisplayParameterOptions.IncludeParamsRefOut
            | SymbolDisplayParameterOptions.IncludeType;
        const SymbolDisplayDelegateStyle delegateStyle = SymbolDisplayDelegateStyle.NameAndSignature;

        readonly static SymbolDisplayFormat format = new SymbolDisplayFormat(
            globalNamespaceStyle: globalStyle,
            typeQualificationStyle: typeQualificationStyle,
            genericsOptions: genericOptions,
            extensionMethodStyle: extensionStyle,
            memberOptions: memberOptions,
            propertyStyle: propertyStyle,
            localOptions: localOptions,
            delegateStyle: delegateStyle,
            parameterOptions: paramOptions);

        internal static JToken GetInfo(CSharpCompilation compilation, string file, string symbolLoc)
        {
            var syntaxTree = compilation.SyntaxTrees.SingleOrDefault(s => s.FilePath == file);
            if (syntaxTree == null)
                return null;

            var parts = symbolLoc.Split(',');
            int start = int.Parse(parts[0]),
                end = int.Parse(parts[1]);

            var root = syntaxTree.GetRoot();
            var maybeToken = FindToken(root, start, end);
            if (!maybeToken.HasValue)
                return null;

            var token = maybeToken.Value;
            return GetTokenInfo(compilation.GetSemanticModel(syntaxTree), token);
        }

        static JToken GetTokenInfo(SemanticModel model, SyntaxToken token)
        {
            var symbol = model.GetDeclaredSymbol(token.Parent);
            if (symbol == null)
            {
                var symbolInfo = model.GetSymbolInfo(token.Parent);

                if (symbolInfo.Symbol == null)
                {
                    symbolInfo = model.GetSpeculativeSymbolInfo(0, token.Parent, SpeculativeBindingOption.BindAsTypeOrNamespace);
                    if (symbolInfo.Symbol == null)
                    {
                        symbolInfo = model.GetSpeculativeSymbolInfo(0, token.Parent, SpeculativeBindingOption.BindAsExpression);
                        if (symbolInfo.Symbol == null)
                            return null;
                    }
                }

                symbol = symbolInfo.Symbol;
            }

            if (!symbol.IsDefinition)
                symbol = symbol.OriginalDefinition;

            string info = null;
            var displayPoarts = symbol.ToDisplayParts(format);
            var xml = symbol.GetDocumentationCommentXml(CultureInfo.InvariantCulture, true);
            if(!string.IsNullOrEmpty(xml))
            {
                try
                {
                    XDocument doc = XDocument.Parse("<doc>" + xml + "</doc>");
                    var summary = doc.Root.Element("summary");
                    if (summary != null)
                        info = summary.Value;
                }
                catch
                { }
            }

            return new JObject(
                new JProperty("summary", info == null ? null : new JValue(info)),
                new JProperty("name", new JArray(
                    displayPoarts.Select(part => Categorize(part))
                ))
            );
        }

        static Regex capital = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        private static JObject Categorize(SymbolDisplayPart part)
        {
            var kind = part.Kind.ToString();
            kind = capital.Replace(kind, "$1-$2").ToLowerInvariant();

            return new JObject(
                new JProperty("kind", new JValue(kind)),
                new JProperty("val", new JValue(part.ToString()))
            );
        }

        static SyntaxToken? FindToken(SyntaxNode node, int start, int end)
        {
            SyntaxNodeOrToken nort = node;
            while (nort.IsNode)
            {
                node = nort.AsNode();
                var startChild = node.ChildThatContainsPosition(start);
                var endChild = node.ChildThatContainsPosition(end - 1);
                if (startChild != endChild)
                    return null;

                nort = startChild;
            }

            if (nort.IsToken)
                return nort.AsToken();
            return null;
        }
    }
}
