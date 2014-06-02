using System.Collections.Immutable;
using Runt.DesignTimeHost.Incomming;

namespace Runt.Core
{
    public class SourcesEventArgs : ProjectEventArgs
    {
        readonly ImmutableList<string> _files;
        readonly ImmutableDictionary<string, string> _generatedFiles;

        public SourcesEventArgs(int contextId, SourcesMessage sources)
            : base(contextId)
        {
            if (sources.Files == null)
                _files = ImmutableList.Create<string>();
            else
                _files = ImmutableList.CreateRange(sources.Files);

            if (sources.GeneratedFiles == null)
                _generatedFiles = ImmutableDictionary.Create<string, string>();
            else
                _generatedFiles = sources.GeneratedFiles.ToImmutableDictionary();
        }

        public ImmutableList<string> Files
        {
            get { return _files; }
        }

        public ImmutableDictionary<string, string> GeneratedFiles
        {
            get { return _generatedFiles; }
        }
    }
}
