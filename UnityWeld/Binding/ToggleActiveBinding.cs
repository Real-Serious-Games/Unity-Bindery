using UnityEngine;
using UnityEngine.Serialization;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Bind to a boolean property on the view model and turn all child objects on
    /// or off based on its value.
    /// </summary>
    [AddComponentMenu("Unity Weld/ToggleActive Binding")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class ToggleActiveBinding : AbstractMemberBinding
    {
        /// <summary>
        /// Type of the adapter we're using to adapt between the view model property 
        /// and view property.
        /// </summary>
        public string ViewAdapterId
        {
            get { return viewAdapterId; }
            set { viewAdapterId = value; }
        }

        [FormerlySerializedAs("viewAdapterTypeName")] [SerializeField]
        private string viewAdapterId;

        /// <summary>
        /// Options for adapting from the view model to the view property.
        /// </summary>
        public AdapterOptions ViewAdapterOptions
        {
            get { return viewAdapterOptions; }
            set { viewAdapterOptions = value; }
        }

        [SerializeField]
        private AdapterOptions viewAdapterOptions;

        /// <summary>
        /// Name of the property in the view model to bind.
        /// </summary>
        public string ViewModelPropertyName
        {
            get { return viewModelPropertyName; }
            set { viewModelPropertyName = value; }
        }

        [SerializeField]
        private string viewModelPropertyName;

        /// <summary>
        /// Watcher the view-model for changes that must be propagated to the view.
        /// </summary>
        private PropertyWatcher viewModelWatcher;

        /// <summary>
        /// Preoprty for the propertySync to set in order to activate and deactivate all children
        /// </summary>
        public bool ChildrenActive
        {
            set
            {
                SetAllChildrenActive(value);
            }
        }


        public override void Connect()
        {
            var viewModelEndPoint = MakeViewModelEndPoint(viewModelPropertyName, null, null);

            var propertySync = new PropertySync(
                // Source
                viewModelEndPoint,

                // Dest
                new PropertyEndPoint(
                    this,
                    "ChildrenActive",
                    TypeResolver.GetAdapter(viewAdapterId),
                    viewAdapterOptions,
                    "view",
                    this
                ),

                // Errors, exceptions and validation.
                null, // Validation not needed

                this
            );

            viewModelWatcher = viewModelEndPoint.Watch(
                () => propertySync.SyncFromSource()
            );

            // Copy the initial value over from the view-model.
            propertySync.SyncFromSource();
        }

        public override void Disconnect()
        {
            if (viewModelWatcher != null)
            {
                viewModelWatcher.Dispose();
                viewModelWatcher = null;
            }
        }

        private void SetAllChildrenActive(bool active)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(active);
            }
        }
    }
}
