using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    public class ReferenceEntry : Entry
    {
        readonly string _name;
        readonly string _version;
        readonly bool _unresolved;
        readonly ImmutableList<ReferenceEntry> _dependencies;

        public ReferenceEntry(string name, string version, bool unresolved, ImmutableList<ReferenceEntry> dependencies)
            : base(null)
        {
            _name = name;
            _version = version;
            _unresolved = unresolved;
            _dependencies = dependencies;
        }

        public override string Key
        {
            get { return "reference:" + _name + ":" + _version; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override string Type
        {
            get { return "reference"; }
        }

        public override IReadOnlyList<Entry> Children
        {
            get { return _dependencies; }
        }

        [JsonIgnore]
        public ImmutableList<ReferenceEntry> Dependencies
        {
            get { return _dependencies; }
        }

        public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
        {
            throw new InvalidOperationException("Cannot set children on a reference node");
        }
    }
}
