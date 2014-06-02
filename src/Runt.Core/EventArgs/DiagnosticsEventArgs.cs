using System.Collections.Immutable;
using Runt.DesignTimeHost.Incomming;

namespace Runt.Core
{
    public class DiagnosticsEventArgs : ProjectEventArgs
    {
        readonly DiagnosticsMessage _message;

        public DiagnosticsEventArgs(int contextId, DiagnosticsMessage message)
            : base(contextId)
        {
            _message = message;
        }

        public IImmutableList<string> Warnings
        {
            get { return _message.Warnings.ToImmutableList(); }
        }

        public IImmutableList<string> Errors
        {
            get { return _message.Errors.ToImmutableList(); }
        }
    }
}
