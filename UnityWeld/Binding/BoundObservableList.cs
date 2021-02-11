using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;


namespace UnityWeld.Binding
{
    /// <summary>
    /// An observable list that is bound to source list.
    /// </summary>
    public class BoundObservableList<DestT, SourceT> : ObservableCollection<DestT>, IDisposable
    {
        /// <summary>
        /// The source list.
        /// </summary>
        private readonly ObservableCollection<SourceT> source;

        /// <summary>
        /// Function that maps source items to dest items.
        /// </summary>
        private readonly Func<SourceT, DestT> itemMap;

        /// <summary>
        /// Callback when new items are added.
        /// </summary>
        private readonly Action<DestT> added;

        /// <summary>
        /// Callback when items are removed.
        /// </summary>
        private readonly Action<DestT> removed;

        /// <summary>
        /// Callback invoked when the collection has changed.
        /// </summary>
        private readonly Action changed;

        /// <summary>
        /// Cache that mimics the contents of the bound list.
        /// This is so we know the items that were cleared when the list is reset.
        /// </summary>
        private readonly List<DestT> cache;

        private bool disposed;

        public BoundObservableList(ObservableCollection<SourceT> source, Func<SourceT, DestT> itemMap) :
            base(source.Select(itemMap))
        {
            this.itemMap = itemMap;
            this.source = source;

            source.CollectionChanged += source_CollectionChanged;
            CollectionChanged += BoundObservableList_CollectionChanged;

            cache = new List<DestT>(this);
        }

        public BoundObservableList(ObservableCollection<SourceT> source, Func<SourceT, DestT> itemMap, Action<DestT> added, Action<DestT> removed) :
            base(source.Select(itemMap))
        {
            if (added == null)
            {
                throw new ArgumentNullException("added", "added must not be null.");
            }
            if (removed == null)
            {
                throw new ArgumentNullException("removed", "removed must not be null.");
            }

            this.itemMap = itemMap;
            this.source = source;
            this.added = added;
            this.removed = removed;

            foreach (var item in this)
            {
                added(item);
            }

            source.CollectionChanged += source_CollectionChanged;
            CollectionChanged += BoundObservableList_CollectionChanged;
            cache = new List<DestT>(this);
        }

        public BoundObservableList(ObservableCollection<SourceT> source, Func<SourceT, DestT> itemMap, Action<DestT> added, Action<DestT> removed, Action changed) :
            base(source.Select(itemMap))
        {
            if (added == null)
            {
                throw new ArgumentNullException("added", "added must not be null.");
            }
            if (removed == null)
            {
                throw new ArgumentNullException("removed", "removed must not be null.");
            }

            this.itemMap = itemMap;
            this.source = source;
            this.added = added;
            this.removed = removed;
            this.changed = changed;

            foreach (var item in this)
            {
                added(item);
            }

            source.CollectionChanged += source_CollectionChanged;
            CollectionChanged += BoundObservableList_CollectionChanged;
            cache = new List<DestT>(this);
        }

        /// <summary>
        /// Event raised when the source collection has changed.
        /// </summary>
        private void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var insertAt = e.NewStartingIndex;

                    foreach (var item in e.NewItems)
                    {
                        var generatedItem = itemMap((SourceT)item);

                        Insert(insertAt, generatedItem);
                        ++insertAt;
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    var removeAt = e.OldStartingIndex;

                    for (var i = 0; i < e.OldItems.Count; i++)
                    {
                        RemoveAt(removeAt);
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Event raised when items are added to the bound list.
        /// </summary>
        private void BoundObservableList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var insertIndex = e.NewStartingIndex;

                    foreach (var item in e.NewItems)
                    {
                        var typedItem = (DestT)item;

                        if (added != null)
                        {
                            added(typedItem);
                        }

                        cache.Insert(insertIndex, typedItem); // Keep the cache updated as new items come in.
                        ++insertIndex;
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        var typedItem = (DestT)item;

                        if (removed != null)
                        {
                            removed(typedItem);
                        }

                        cache.RemoveAt(e.OldStartingIndex); // Keep the cache updated as items are removed.
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (removed != null)
                    {
                        foreach (var item in cache)
                        {
                            removed(item);
                        }
                    }
                    cache.Clear();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (changed != null)
            {
                changed.Invoke();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                source.CollectionChanged -= source_CollectionChanged;
                CollectionChanged -= BoundObservableList_CollectionChanged;
            }

            disposed = true;
        }
    }
}
