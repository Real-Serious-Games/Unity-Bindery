using System.Reflection;
using UnityEngine;
using UnityWeld.Binding.Exceptions;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Binds to a view and instantiates a template based on the view type.
    /// </summary>
    [AddComponentMenu("Unity Weld/Template Binding")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class TemplateBinding : AbstractTemplateSelector
    {
        /// <summary>
        /// The property of the view model that is being bound to
        /// </summary>
        private PropertyInfo viewModelProperty;
        
        /// <summary>
        /// Connect to the attached view model.
        /// </summary>
        public override void Connect()
        {
            Disconnect();

            string propertyName;
            ParseViewModelEndPointReference(
                ViewModelPropertyName, 
                out propertyName, 
                out ViewModel
            );

            ViewModelPropertyWatcher = new PropertyWatcher(
                ViewModel, 
                propertyName, 
                InitializeTemplate
            );

            // Get property from view model.
            viewModelProperty = ViewModel.GetType().GetProperty(propertyName);
            if (viewModelProperty == null)
            {
                throw new MemberNotFoundException(string.Format(
                    "Expected property {0} on type {1}, but was not found.", 
                    propertyName, 
                    ViewModel.GetType().Name
                ));
            }

            InitializeTemplate();
        }


        /// <summary>
        /// Disconnect from the attached view model.
        /// </summary>
        public override void Disconnect()
        {
            DestroyAllTemplates();

            if (ViewModelPropertyWatcher != null)
            {
                ViewModelPropertyWatcher.Dispose();
                ViewModelPropertyWatcher = null;
            }

            ViewModel = null;
        }

        /// <summary>
        /// Initialized the correct template based on the type.
        /// </summary>
        private void InitializeTemplate()
        {
            DestroyAllTemplates();

            // Get value from view model.
            var viewModelPropertyValue = viewModelProperty.GetValue(ViewModel, null);
            if (viewModelPropertyValue == null)
            {
                throw new PropertyNullException($"TemplateBinding cannot bind to null property in view: {ViewModelPropertyName}.");
            }

            InstantiateTemplate(viewModelPropertyValue);
        }
    }
}
