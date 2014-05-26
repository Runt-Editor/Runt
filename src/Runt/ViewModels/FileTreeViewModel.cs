using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Caliburn.Micro;

namespace Runt.ViewModels
{
    public abstract class FileTreeViewModel : PropertyChangedBase, INotifyCollectionChanged, IList
    {
        private readonly FileTreeViewModel _parent;

        private IList _items;
        private NotifyCollectionChangedEventHandler _collectionChanged;

        private bool _isSelected;
        private bool _isExpanded;

        public FileTreeViewModel(FileTreeViewModel parent)
        {
            Contract.Requires(this is WorkspaceViewModel ^ parent != null);

            _parent = parent;
        }

        protected abstract bool HasItems { get; }
        protected abstract IEnumerable GetItems();
        protected abstract void Initialize();

        public abstract string Name { get; }
        public abstract ImageSource Icon { get; }

        public virtual WorkspaceViewModel Workspace
        {
            get { return _parent.Workspace; }
        }

        public virtual ProjectViewModel Project
        {
            get { return _parent == null ? null : _parent.Project; }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if(_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange(() => IsSelected);
                }
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if(_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(() => IsExpanded);

                    if (_isExpanded && _parent != null)
                        _parent.IsExpanded = true;
                }
            }
        }

        private IList Items
        {
            get
            {
                if (_items == null)
                    Interlocked.CompareExchange(ref _items, (HasItems ? GetItems().Cast<object>() : Enumerable.Empty<object>()).ToList(), null);
                return _items;
            }
        }

        /// <summary>
        /// Raises a change notification indicating that all bindings should be refreshed.
        /// </summary>
        protected new void Refresh()
        {
            _items = null;
            Execute.OnUIThread(() =>
            {
                OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            });
        }

        protected void InsertItem(int index, object value)
        {
            Execute.OnUIThread(() =>
            {
                Items.Insert(index, value);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index));
            });
        }

        protected void SetItem(int index, object value)
        {
            Execute.OnUIThread(() => {
                Items[index] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, index));
            });
        }

        protected void RemoveItem(int index)
        {
            Execute.OnUIThread(() =>
            {
                var value = Items[index];
                Items.RemoveAt(index);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value, index));
            });
        }

        protected void InsertRange(int index, IEnumerable items)
        {
            var itms = items.Cast<object>().ToList();
            Execute.OnUIThread(() =>
            {
                for(var i = 0; i < itms.Count; i++)
                    Items.Insert(index + i, itms[i]);

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itms, index));
            });
        }

        protected void RemoveRange(int index, int count)
        {
            Execute.OnUIThread(() =>
            {
                var items = new List<object>();
                for (var i = 0; i < count; i++)
                {
                    items.Add(Items[i]);
                    Items.RemoveAt(index);
                }

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, index));
            });
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (IsNotifying)
            {
                var cc = _collectionChanged;
                if (cc != null)
                    Execute.OnUIThread(() => cc(this, e));
            }
        }

        #region Implementations

        object IList.this[int index]
        {
            get
            {
                return Items[index];
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        int ICollection.Count
        {
            get
            {
                return Items.Count;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return null;
            }
        }

        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add
            {
                while(true)
                {
                    var cc = Volatile.Read(ref _collectionChanged);
                    var n = cc + value;
                    if (Interlocked.CompareExchange(ref _collectionChanged, n, cc) == cc)
                        break;
                }
            }

            remove
            {
                while (true)
                {
                    var cc = Volatile.Read(ref _collectionChanged);
                    var n = cc - value;
                    if (Interlocked.CompareExchange(ref _collectionChanged, n, cc) == cc)
                        break;
                }
            }
        }


        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value)
        {
            return Items.Contains(value);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            Items.CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        int IList.IndexOf(object value)
        {
            return Items.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
