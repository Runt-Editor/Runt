using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Runt.Service.SourceServices
{
    class IndexIdTranslator
    {
        // This is copy-pasted from Kirill's source to ensure correct hashes
        public static ISymbol GetTargetSymbol(ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.Parameter || symbol.Kind == SymbolKind.Local)
                symbol = symbol.ContainingSymbol;

            if (IsThisParameter(symbol))
            {
                var paramType = ((IParameterSymbol)symbol).Type;
                if (paramType != null)
                    return paramType;
            }
            else if (IsFunctionValue(symbol))
            {
                var method = symbol.ContainingSymbol as IMethodSymbol;
                if (method != null)
                {
                    if (method.AssociatedSymbol != null)
                    {
                        return method.AssociatedSymbol;
                    }
                    else
                    {
                        return method;
                    }
                }
            }

            if (symbol.Kind == SymbolKind.Method)
                return ((IMethodSymbol)symbol).ReducedFrom ?? symbol;

            symbol = ResolveAccessorParameter(symbol);

            return symbol;
        }

        private static ISymbol ResolveAccessorParameter(ISymbol symbol)
        {
            if (symbol == null || !symbol.IsImplicitlyDeclared)
            {
                return symbol;
            }

            var parameterSymbol = symbol as IParameterSymbol;
            if (parameterSymbol == null)
            {
                return symbol;
            }

            var accessorMethod = parameterSymbol.ContainingSymbol as IMethodSymbol;
            if (accessorMethod == null)
            {
                return symbol;
            }

            var property = accessorMethod.AssociatedSymbol as IPropertySymbol;
            if (property == null)
            {
                return symbol;
            }

            int ordinal = parameterSymbol.Ordinal;
            if (property.Parameters.Length <= ordinal)
            {
                return symbol;
            }

            return property.Parameters[ordinal];
        }

        private static bool IsFunctionValue(ISymbol symbol)
        {
            return symbol is ILocalSymbol && ((ILocalSymbol)symbol).IsFunctionValue;
        }

        private static bool IsThisParameter(ISymbol symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)symbol).IsThis;
        }

        private static string GetLocalDocumentationComment(ISymbol localDefinition)
        {
            if (localDefinition.ContainingSymbol == null)
                return null;

            var methodSymbol = GetDocumentationCommentId(localDefinition.ContainingSymbol);
            return methodSymbol + "|" + "L:" + localDefinition.Name;
        }

        private static string GetDocumentationCommentId(ISymbol symbol)
        {
            string result = null;
            if (!symbol.IsDefinition)
            {
                symbol = symbol.OriginalDefinition;
            }

            if (symbol is ILocalSymbol)
                return GetLocalDocumentationComment(symbol);

            result = symbol.GetDocumentationCommentId();

            result = result.Replace("#ctor", "ctor");

            return result;
        }
        public static string GetId(ISymbol symbol)
        {
            return GetDocumentationCommentId(symbol);
        }
    }
}
