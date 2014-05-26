using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Runt.DesignTimeHost;

namespace Runt.ViewModels
{
    [ProxyModel(typeof(FileTreeViewModel))]
    public class ReferencesViewModel : FileTreeViewModel
    {
        ReferencesEventArgs _references;
        ConcurrentDictionary<string, ReferenceViewModel> _cache = new ConcurrentDictionary<string, ReferenceViewModel>();

        public ReferencesViewModel(ProjectViewModel project)
            : base(project)
        {

        }

        public override string Name
        {
            get { return "References"; }
        }

        protected override bool HasItems
        {
            get { return true; }
        }

        protected override IEnumerable GetItems()
        {
            if (_references == null)
                return Enumerable.Empty<object>();

            return GetReference(_references.Root).GetItemsInternal();
        }

        private ReferenceViewModel GetReference(string name)
        {
            return _cache.GetOrAdd(name, n => new ReferenceViewModel(this, _references.Packages[n]));
        }

        protected override void Initialize()
        {
            // do nothing
        }

        protected override void Refresh()
        {
            _cache = new ConcurrentDictionary<string, ReferenceViewModel>();
            base.Refresh();
        }

        internal void Update(ReferencesEventArgs e)
        {
            _references = e;
            Refresh();
        }

        public override ImageSource Icon
        {
            get { return Icons.Reference; }
        }

        [ProxyModel(typeof(FileTreeViewModel))]
        public class ReferenceViewModel : FileTreeViewModel
        {
            readonly ReferencesEventArgs.Package _dep;
            readonly ReferencesViewModel _parent;

            public ReferenceViewModel(ReferencesViewModel parent, ReferencesEventArgs.Package dep)
                : base(parent)
            {
                _dep = dep;
                _parent = parent;
            }

            public override ImageSource Icon
            {
                get { return Icons.Reference; }
            }

            public override string Name
            {
                get { return _dep.Name; }
            }

            protected override bool HasItems
            {
                get { return true; }
            }

            internal IEnumerable GetItemsInternal()
            {
                return GetItems();
            }

            protected override IEnumerable GetItems()
            {
                return from d in _dep.Dependencies
                       orderby d.Name
                       select _parent.GetReference(d.Name);
            }

            protected override void Initialize()
            {
                // do nothing
            }
        }
    }
}
