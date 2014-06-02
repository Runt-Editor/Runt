using System.Collections.Immutable;
using Runt.DesignTimeHost.Incomming;

namespace Runt.Core
{
    public class SourcesEventArgs : ProjectEventArgs
    {
        readonly SourcesMessage _sources;

        public SourcesEventArgs(int contextId, SourcesMessage sources)
            : base(contextId)
        {
            _sources = sources;
        }

        public IImmutableList<string> Files
        {
            get { return _sources.Files.ToImmutableList(); }
        }

        public IImmutableDictionary<string, string> GeneratedFiles
        {
            get { return _sources.GeneratedFiles.ToImmutableDictionary(); }
        }
    }
}
