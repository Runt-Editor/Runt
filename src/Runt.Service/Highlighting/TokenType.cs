using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Runt.Service.Highlighting
{
    public class TokenType
    {
        public enum Kind
        {
            Unknown,
            Reference,
            Declaration
        }

        readonly Kind _kind;
        readonly ISymbol _symbol;

        private TokenType(Kind kind, ISymbol symbol)
        {
            _kind = kind;
            _symbol = symbol;
        }

        internal static TokenType Reference(ISymbol symbol)
        {
            return new TokenType(Kind.Reference, symbol);
        }

        internal static TokenType Unknown()
        {
            return new TokenType(Kind.Unknown, null);
        }

        internal static TokenType Declaration(ISymbol declarationSymbol)
        {
            return new TokenType(Kind.Declaration, declarationSymbol);
        }

        public Kind Type
        {
            get { return _kind; }
        }

        public ISymbol Symbol
        {
            get { return _symbol; }
        }
    }
}
