using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Runt.Core;

namespace Runt.Service.CompilationModel
{
    class ProjectCompilation
    {
        public static ProjectCompilation Create(string name, int id)
        {
            return new ProjectCompilation(CSharpCompilation.Create(name), id, ImmutableDictionary.Create<int, int>(), ValidItems.None, 0);
        }

        [Flags]
        enum ValidItems
        {
            None = 0,
            References = 1,
            Sources = 2,
            Configuration = 4,

            All = References | Sources | Configuration
        }

        readonly CSharpCompilation _compilation;
        readonly ImmutableDictionary<int, int> _projectReferences;
        readonly ValidItems _valid;
        readonly int _version = 0;
        readonly int _id;

        private ProjectCompilation(CSharpCompilation compilation, int id, ImmutableDictionary<int, int> projectReferences, ValidItems valid, int version)
        {
            Contract.Requires(projectReferences != null);

            _compilation = compilation;
            _valid = valid;
            _version = version;
            _id = id;
            _projectReferences = projectReferences;
        }

        public ProjectCompilation InvalidateReferences()
        {
            if ((_valid & ValidItems.References) == ValidItems.References)
                return new ProjectCompilation(_compilation.RemoveAllReferences(), _id,
                    _projectReferences, _valid & (~ValidItems.References), _version + 1);

            return this;
        }

        public ProjectCompilation InvalidateSources()
        {
            if ((_valid & ValidItems.Sources) == ValidItems.Sources)
                return new ProjectCompilation(_compilation.RemoveAllSyntaxTrees(), _id,
                    _projectReferences, _valid & (~ValidItems.Sources), _version + 1);

            return this;
        }

        public ProjectCompilation InvalidateConfiguration()
        {
            if ((_valid & ValidItems.Configuration) == ValidItems.Configuration)
                return new ProjectCompilation(_compilation.RemoveAllReferences().RemoveAllSyntaxTrees(),
                    _id, ImmutableDictionary.Create<int, int>(), ValidItems.None, _version + 1);

            return this;
        }

        public bool IsValid
        {
            get { return _valid == (ValidItems.All); }
        }

        public bool NeedsReferences
        {
            get { return (_valid & ValidItems.References) == (ValidItems)0; }
        }

        public bool NeedsSources
        {
            get { return (_valid & ValidItems.Sources) == (ValidItems)0; }
        }

        public bool NeedsConfiguration
        {
            get { return (_valid & ValidItems.Configuration) == (ValidItems)0; }
        }

        public int Version
        {
            get { return _version; }
        }

        public int Id
        {
            get { return _id; }
        }

        public ImmutableDictionary<int, int> ProjectReferences
        {
            get { return _projectReferences; }
        }

        public MetadataReference GetMetadataReference()
        {
            return _compilation.ToMetadataReference();
        }

        internal void GetRootForFile(string path, out SyntaxNode root, out SemanticModel model)
        {
            foreach (var syntaxTree in _compilation.SyntaxTrees)
            {
                if (syntaxTree.FilePath == path)
                {
                    root = syntaxTree.GetRoot();
                    model = _compilation.GetSemanticModel(syntaxTree);
                    return;
                }
            }


            throw new ArgumentException("SyntaxTree with given path not found");
        }

        internal ProjectCompilation Update(Dictionary<int, int> projRefs, List<MetadataReference> references, List<SyntaxTree> sources)
        {
            Contract.Ensures(Contract.Result<ProjectCompilation>().IsValid);

            ValidItems valid = _valid;
            var compilation = _compilation;
            if (references != null)
            {
                compilation = compilation.RemoveAllReferences()
                    .AddReferences(references);
                valid |= ValidItems.References;
            }

            if (sources != null)
            {
                compilation = compilation.RemoveAllSyntaxTrees()
                    .AddSyntaxTrees(sources);
                valid |= ValidItems.Sources | ValidItems.Configuration;
            }

            return new ProjectCompilation(
                compilation, _id, 
                (projRefs ?? new Dictionary<int, int>()).ToImmutableDictionary(),
                valid, _version + 1);
        }

        internal CSharpCompilation Compilation
        {
            get { return _compilation; }
        }
    }
}
