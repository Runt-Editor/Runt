using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Runt.DesignTimeHost.Incomming;

namespace Runt.Core
{
    public class ReferencesEventArgs : ProjectEventArgs
    {
        readonly string _root;
        readonly IImmutableDictionary<string, Package> _dependencies;

        public ReferencesEventArgs(int contextId, ReferencesMessage message)
            : base(contextId)
        {
            _root = message.RootDependency;
            _dependencies = message.Dependencies.Select(kvp => new KeyValuePair<string, Package>(kvp.Key, new Package(kvp.Value))).ToImmutableDictionary();
        }

        public string Root
        {
            get { return _root; }
        }

        public IImmutableDictionary<string, Package> Packages
        {
            get { return _dependencies; }
        }

        public class Package
        {
            readonly ReferenceDescription _ref;

            public Package(ReferenceDescription desc)
            {
                _ref = desc;
            }

            public string Name
            {
                get { return _ref.Name; }
            }

            public bool Unresolved
            {
                get { return _ref.Type == "Unresolved"; }
            }

            public string Version
            {
                get { return _ref.Version; }
            }

            public IImmutableList<Dependency> Dependencies
            {
                get { return _ref.Dependencies.Select(d => new Dependency(d)).ToImmutableArray(); }
            }
        }

        public class Dependency
        {
            readonly string _name;
            readonly string _version;

            public Dependency(ReferenceItem r)
            {
                _name = r.Name;
                _version = r.Version;
            }

            public string Name
            {
                get { return _name; }
            }

            public string Version
            {
                get { return _version; }
            }
        }
    }
}
