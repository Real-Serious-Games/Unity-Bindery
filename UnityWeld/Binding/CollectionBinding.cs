using System;
using System.Collections;
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
        /// <summary>
        /// Collection that we have bound to.
        /// </summary>
        private IEnumerable viewModelCollectionValue;
        
        public override void Connect()
        {
            Disconnect();

            string propertyName;
            object newViewModel;
            ParseViewModelEndPointReference(
                ViewModelPropertyName, 
                out propertyName, 
                out newViewModel
            );

            viewModel = newViewModel;

            viewModelPropertyWatcher = new PropertyWatcher(
                newViewModel, 
                propertyName, 
                NotifyPropertyChanged_PropertyChanged
            );

            BindCollection();
        }

        public override void Disconnect()
        {
            UnbindCollection();

            if (viewModelPropertyWatcher != null)
            {
                viewModelPropertyWatcher.Dispose();
                viewModelPropertyWatcher = null;
            }

            viewModel = null;
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
                        var list = viewModelCollectionValue as IList;

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
            var viewModelType = viewModel.GetType();

            string propertyName;
            string viewModelName;
            ParseEndPointReference(
                ViewModelPropertyName, 
                out propertyName, 
                out viewModelName
            );

            var viewModelCollectionProperty = viewModelType.GetProperty(propertyName);
            if (viewModelCollectionProperty == null)
            {
                throw new MemberNotFoundException(
                    "Expected property " 
                    + ViewModelPropertyName + ", but it wasn't found on type " 
                    + viewModelType + "."
                );
            }

            // Get value from view model.
            var viewModelValue = viewModelCollectionProperty.GetValue(viewModel, null);
            if (viewModelValue == null)
            {
                throw new PropertyNullException(
                    "Cannot bind to null property in view: " 
                    + ViewModelPropertyName
                );
            }

            viewModelCollectionValue = viewModelValue as IEnumerable;
            if (viewModelCollectionValue == null)
            {
                throw new InvalidTypeException(
                    "Property " 
                    + ViewModelPropertyName 
                    + " is not a collection and cannot be used to bind collections."
                );
            }

            // Generate children
            var collectionAsList = viewModelCollectionValue.Cast<object>().ToList();
            for (var index = 0; index < collectionAsList.Count; index++)
            {
                InstantiateTemplate(collectionAsList[index], index);
            }

            // Subscribe to collection changed events.
            var collectionChanged = viewModelCollectionValue as INotifyCollectionChanged;
            if (collectionChanged != null)
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
            if (viewModelCollectionValue != null)
            {
                var collectionChanged = viewModelCollectionValue as INotifyCollectionChanged;
                if (collectionChanged != null)
                {
                    collectionChanged.CollectionChanged -= Collection_CollectionChanged;
                }

                viewModelCollectionValue = null;
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

    }
}
