using UnityEngine;
using UnityEngine.Serialization;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Bind a property in the view model to one on the UI, subscribing to OnPropertyChanged 
    /// and updating the UI accordingly (note that this does not update the view model when
    /// the UI changes).
    /// </summary>
    [AddComponentMenu("Unity Weld/OneWay Property Binding")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class OneWayPropertyBinding : AbstractMemberBinding
    {
        /// <summary>
        /// Type of the adapter we're using to adapt between the view model property 
        /// and UI property.
        /// </summary>
        public string ViewAdapterId
        {
            get { return viewAdapterId; }
            set { viewAdapterId = value; }
        }

        [FormerlySerializedAs("viewAdapterTypeName")] [SerializeField]
        private string viewAdapterId;

        /// <summary>
        /// Options for adapting from the view model to the UI property.
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
        /// Property on the view to update when value changes.
        /// </summary>
        public string ViewPropertyName
        {
            get { return viewPropertyName; }
            set { viewPropertyName = value; }
        }

        [SerializeField, FormerlySerializedAs("uiPropertyName")]
        private string viewPropertyName;

        /// <summary>
        /// Watches the view-model for changes that must be propagated to the view.
        /// </summary>
        private PropertyWatcher viewModelWatcher;

        public override void Connect()        
        {
            string propertyName;
            Component view;
            ParseViewEndPointReference(viewPropertyName, out propertyName, out view);

            var viewModelEndPoint = MakeViewModelEndPoint(viewModelPropertyName, null, null);

            var propertySync = new PropertySync(
                // Source
                viewModelEndPoint,

                // Dest
                new PropertyEndPoint(
                    view,
                    propertyName,
                    TypeResolver.GetAdapter(ViewAdapterId),
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
    }
}
