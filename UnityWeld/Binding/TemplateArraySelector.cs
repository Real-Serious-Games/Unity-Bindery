﻿using UnityEngine;
using System.Collections;
using UnityWeld.Binding;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Binds to a view and instantiates a template based on the view type.
    /// </summary>
    public class TemplateArraySelector : AbstractMemberBinding
    {
        /// <summary>
        /// The name of the property we are binding to on the view model.
        /// </summary>
        public string viewModelPropertyName = string.Empty;

        /// <summary>
        /// The gameobject in the scene that is the parent object for the tenplates.
        /// </summary>
        public GameObject templates;

        /// <summary>
        /// All available templates indexed by the view model the are for.
        /// </summary>
        private IDictionary<string, TemplateBinding> availableTemplates = new Dictionary<string, TemplateBinding>();

        /// <summary>
        /// The templates that have been instantiated.
        /// </summary>
        private List<TemplateBinding> initalizedTemplates = null;

        /// <summary>
        /// The view-model, cached during connection.
        /// </summary>
        private object viewModel;

        /// <summary>
        /// The property of the view model that is being bound to
        /// </summary>
        private PropertyInfo viewModelProperty = null;

        /// <summary>
        /// Watches the view-model property for changes.
        /// </summary>
        private PropertyWatcher viewModelPropertyWatcher;

        /// <summary>
        /// Connect to the attached view model.
        /// </summary>
        public override void Connect()
        {
            Disconnect();

            // Cache available templates.
            var templatesInScene = templates.GetComponentsInChildren<TemplateBinding>(true);
            foreach (var template in templatesInScene)
            {
                availableTemplates.Add(template.GetViewModelTypeName(), template);
            }

            string propertyName;
            object viewModel;
            ParseViewModelEndPointReference(viewModelPropertyName, out propertyName, out viewModel);

            this.viewModel = viewModel;

            viewModelPropertyWatcher = new PropertyWatcher(viewModel, propertyName,
                () => InitalizeTemplate()
            );

            // Get property from view model.
            viewModelProperty = viewModel.GetType().GetProperty(propertyName);
            if (viewModelProperty == null)
            {
                throw new ApplicationException("Expected property " + viewModelPropertyName + ", but was not found.");
            }

            InitalizeTemplate();
        }


        /// <summary>
        /// Disconnect from the attached view model.
        /// </summary>
        public override void Disconnect()
        {
            DestroyTemplate();
            availableTemplates.Clear();

            if (viewModelPropertyWatcher != null)
            {
                viewModelPropertyWatcher.Dispose();
                viewModelPropertyWatcher = null;
            }

            viewModel = null;
        }

        /// <summary>
        /// Initalized the correct tempalte based on the type.
        /// </summary>
        private void InitalizeTemplate()
        {
            DestroyTemplate();

            //Get values from view model array
            Array a = (Array)viewModelProperty.GetValue(viewModel, null);
            //Go through each value
            if (a != null)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    // Get value from view model.
                    var viewModelPropertyValue = a.GetValue(i);
                    if (viewModelPropertyValue == null)
                    {
                        throw new ApplicationException("Cannot bind to null property in view: " + viewModelPropertyName);
                    }

                    initalizedTemplates = new List<TemplateBinding>();

                    TemplateBinding initalizedTemplate = null;

                    // Select template.
                    var viewModelValueType = viewModelPropertyValue.GetType().Name;
                    TemplateBinding selectedTemplate = null;
                    if (!availableTemplates.TryGetValue(viewModelValueType, out selectedTemplate))
                    {
                        throw new ApplicationException("Cannot find matching template for: " + viewModelValueType);
                    }

                    // Setup selected template.
                    initalizedTemplate = Instantiate(selectedTemplate);

                    initalizedTemplate.transform.SetParent(transform, false);

                    // Set up child bindings before we activate the template object so that they will be configured properly before trying to connect.
                    initalizedTemplate.InitChildBindings(viewModelPropertyValue);

                    initalizedTemplate.gameObject.SetActive(true);

                    initalizedTemplates.Add(initalizedTemplate);
                }
            }
        }

        /// <summary>
        /// Destroys the instantiated template.
        /// </summary>
        private void DestroyTemplate()
        {
            if (initalizedTemplates == null)
            {
                return;
            }

            foreach (TemplateBinding tb in initalizedTemplates)
            {
                Destroy(tb.gameObject);
            }
            initalizedTemplates.Clear();
            initalizedTemplates = null;
        }
    }
}
