using UnityEngine;
using UnityWeld.Binding.Exceptions;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Bind a sub-view model which is a property on another view model for use in the UI.
    /// </summary>
    [AddComponentMenu("Unity Weld/SubViewModel Binding")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class SubViewModelBinding : AbstractMemberBinding, IViewModelProvider
    {
        /// <summary>
        /// Get the view-model provided by this provider.
        /// </summary>
        public object GetViewModel()
        {
            if(viewModel == null)
            {
                Connect();
            }

            return viewModel;
        }

        /// <summary>
        /// Get the name of the view-model's type.
        /// </summary>
        public string GetViewModelTypeName()
        {
            return viewModelTypeName;
        }

        /// <summary>
        /// Name of the property in the view-model that contains the sub-viewmodel.
        /// </summary>
        public string ViewModelPropertyName
        {
            get { return viewModelPropertyName; }
            set { viewModelPropertyName = value; }
        }

        [SerializeField] private string viewModelPropertyName;

        /// <summary>
        /// Name of the type of the view model we're binding to. Set from the Unity inspector.
        /// </summary>
        public string ViewModelTypeName
        {
            get { return viewModelTypeName; }
            set { viewModelTypeName = value; }
        }

        [SerializeField] private string viewModelTypeName;

        /// <summary>
        /// Watches the view-model proper for changes.
        /// </summary>
        private PropertyWatcher viewModelPropertyWatcher;

        /// <summary>
        /// Cached view-model object.
        /// </summary>
        private object viewModel;

        /// <summary>
        /// Initialise the bound view model by getting the property from the parent view model.
        /// </summary>
        private void UpdateViewModel()
        {
            ParseViewModelEndPointReference(viewModelPropertyName, out var propertyName, out var parentViewModel);

            var propertyInfo = parentViewModel.GetType().GetProperty(propertyName);
            if(propertyInfo == null)
            {
                throw new MemberNotFoundException(
                    $"Could not find property \"{propertyName}\" on view model \"{parentViewModel.GetType()}\".");
            }

            viewModel = propertyInfo.GetValue(parentViewModel, null);
        }

        public override void Connect()
        {
            if(viewModelPropertyWatcher != null)
            {
                // Already connected - no need to connect again.
                return;
            }

            ParseViewModelEndPointReference(viewModelPropertyName, out var propertyName, out var parentViewModel);

            viewModelPropertyWatcher =
                new PropertyWatcher(parentViewModel, propertyName, NotifyPropertyChanged_PropertyChanged);

            UpdateViewModel();
        }

        public override void Disconnect()
        {
            if(viewModelPropertyWatcher != null)
            {
                viewModelPropertyWatcher.Dispose();
                viewModelPropertyWatcher = null;
            }
        }

        private void NotifyPropertyChanged_PropertyChanged()
        {
            UpdateViewModel();

            // Rebind all children.
            using(var cache = gameObject.GetComponentsWithCache<AbstractMemberBinding>())
            {
                foreach(var memberBinding in cache.Components)
                {
                    if(memberBinding == this)
                    {
                        continue;
                    }

                    memberBinding.Init();
                }
            }
        }
    }
}