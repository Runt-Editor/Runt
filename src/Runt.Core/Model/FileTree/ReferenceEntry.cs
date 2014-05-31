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

        public ReferenceEntry(string rel, bool isOpen, string name, string version, bool unresolved, ImmutableList<ReferenceEntry> dependencies)
            : base(rel, isOpen)
        {
            _name = name;
            _version = version;
            _unresolved = unresolved;
            _dependencies = dependencies;
        }

        public override Entry AsOpen(bool open, JObject changes)
        {
            RegisterOpenChange(open, changes);
            return new ReferenceEntry(RelativePath, open, _name, _version, _unresolved, _dependencies);
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
            var indexChange = new JObject();
            Utils.RegisterChange(indexChange, "0", child, subChange);

            // Note: I use null here because I don't want to create the lists.
            // given that indexChange will never be null, this is safe.
            Utils.RegisterChange(changes, () => Children, null, indexChange);
            return new ReferenceEntry(RelativePath, IsOpen, _name, _version, _unresolved, _dependencies.SetItem(index, (ReferenceEntry)child));
        }
    }
}
