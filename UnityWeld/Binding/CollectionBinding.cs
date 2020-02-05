using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityWeld.Binding.Exceptions;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Binds a property in the view-model that is a collection and instantiates copies
    /// of template objects to bind to the items of the collection.
    /// 
    /// Creates and destroys child objects when items are added and removed from a 
    /// collection that implements INotifyCollectionChanged, like ObservableList.
    /// </summary>
    [AddComponentMenu("Unity Weld/Collection Binding")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class CollectionBinding : AbstractTemplateSelector
    {
        private readonly IDictionary<string, Queue<Template>> _pool = new Dictionary<string, Queue<Template>>();

        /// <summary>
        /// Collection that we have bound to.
        /// </summary>
        private IEnumerable _viewModelCollectionValue;

        [SerializeField]
        private Transform _itemsContainer;
        [SerializeField] 
        private int _templateInitialPoolCount = 0;

        public Transform ItemsContainer
        {
            get => _itemsContainer;
            set => _itemsContainer = value;
        }

        public int TemplateInitialPoolCount
        {
            get => _templateInitialPoolCount;
            set => _templateInitialPoolCount = value;
        }

        protected Transform Container => _itemsContainer ? _itemsContainer : transform;

        public override void Connect()
        {
            Disconnect();

            ParseViewModelEndPointReference(
                ViewModelPropertyName, 
                out var propertyName, 
                out var newViewModel
            );

            ViewModel = newViewModel;

            ViewModelPropertyWatcher = new PropertyWatcher(
                newViewModel, 
                propertyName, 
                NotifyPropertyChanged_PropertyChanged
            );

            BindCollection();
        }

        public override void Disconnect()
        {
            UnbindCollection();

            if (ViewModelPropertyWatcher != null)
            {
                ViewModelPropertyWatcher.Dispose();
                ViewModelPropertyWatcher = null;
            }

            ViewModel = null;
        }

        private void NotifyPropertyChanged_PropertyChanged()
        {
            RebindCollection();
        }

        private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // Add items that were added to the bound collection.
                    if (e.NewItems != null)
                    {
                        var list = _viewModelCollectionValue as IList;

                        foreach (var item in e.NewItems)
                        {
                            int index;
                            if (list == null)
                            {
                                // Default to adding the new object at the last index.
                                index = transform.childCount;
                            }
                            else
                            {
                                index = list.IndexOf(item);
                            }
                            InstantiateTemplate(item, index);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    // TODO: respect item order
                    // Remove items that have been deleted.
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            DestroyTemplate(item);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    DestroyAllTemplates();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Bind to the view model collection so we can monitor it for changes.
        /// </summary>
        private void BindCollection()
        {
            // Bind view model.
            var viewModelType = ViewModel.GetType();

            ParseEndPointReference(ViewModelPropertyName, out var propertyName, out _);

            var viewModelCollectionProperty = viewModelType.GetProperty(propertyName);
            if (viewModelCollectionProperty == null)
            {
                throw new MemberNotFoundException(
                    $"Expected property {ViewModelPropertyName}, but it wasn't found on type {viewModelType}.");
            }

            // Get value from view model.
            var viewModelValue = viewModelCollectionProperty.GetValue(ViewModel, null);
            if (viewModelValue == null)
            {
                return;
            }

            _viewModelCollectionValue = viewModelValue as IEnumerable;
            if (_viewModelCollectionValue == null)
            {
                throw new InvalidTypeException(
                    $"Property {ViewModelPropertyName} is not a collection and cannot be used to bind collections.");
            }

            // Generate children
            var collectionAsList = _viewModelCollectionValue.Cast<object>().ToList();
            for (var index = 0; index < collectionAsList.Count; index++)
            {
                InstantiateTemplate(collectionAsList[index], index);
            }

            // Subscribe to collection changed events.
            if (_viewModelCollectionValue is INotifyCollectionChanged collectionChanged)
            {
                collectionChanged.CollectionChanged += Collection_CollectionChanged;
            }
        }

        /// <summary>
        /// Unbind from the collection, stop monitoring it for changes.
        /// </summary>
        private void UnbindCollection()
        {
            DestroyAllTemplates();

            // Unsubscribe from collection changed events.
            if (_viewModelCollectionValue != null)
            {
                var collectionChanged = _viewModelCollectionValue as INotifyCollectionChanged;
                if (collectionChanged != null)
                {
                    collectionChanged.CollectionChanged -= Collection_CollectionChanged;
                }

                _viewModelCollectionValue = null;
            }
        }

        /// <summary>
        /// Rebind to the collection when it has changed on the view-model.
        /// </summary>
        private void RebindCollection()
        {
            UnbindCollection();
            BindCollection();
        }

        protected override Template CloneTemplate(Template template)
        {
            if(_pool.TryGetValue(template.ViewModelTypeName, out var pool) && pool.Count > 0)
            {
                return  pool.Dequeue();
            }

            return base.CloneTemplate(template);
        }

        protected override void OnTemplateDestroy(Template template)
        {
            PutTemplateToPool(template);
        }

        private void PutTemplateToPool(Template template)
        {
            template.gameObject.SetActive(false);
            template.SetBindings(false);

            if(!_pool.TryGetValue(template.ViewModelTypeName, out var pool))
            {
                _pool.Add(template.ViewModelTypeName, pool = new Queue<Template>());
            }

            pool.Enqueue(template);
        }

        public void WarmUpTemplates()
        {
            foreach(var template in AvailableTemplates.Values)
            {
                for(var i = 0; i < TemplateInitialPoolCount; i++)
                {
                    var clone = Instantiate(template, Container);
                    PutTemplateToPool(clone);

                    foreach(var collectionBinding in BindingHelpers.IterateComponents<CollectionBinding>(clone.gameObject))
                    {
                        collectionBinding.WarmUpTemplates();
                    }
                }
            }
        }
    }
}
