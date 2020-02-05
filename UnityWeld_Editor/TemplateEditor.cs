using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityWeld.Binding;
using UnityWeld.Binding.Internal;

namespace UnityWeld_Editor
{
    /// <summary>
    /// Editor for template bindings with a dropdown for selecting what view model
    /// to bind to.
    /// </summary>
    [CustomEditor(typeof(Template))]
    public class TemplateEditor : BaseBindingEditor
    {
        private Template targetScript;

        /// <summary>
        /// Whether the value on our target matches its prefab.
        /// </summary>
        private bool propertyPrefabModified;

        protected override void OnEnabled()
        {
            targetScript = (Template)target;
        }

        protected override void OnInspector()
        {
            UpdatePrefabModifiedProperties();

            var availableViewModels = TypeResolver.TypesWithBindingAttribute
                .Select(type => type.ToString())
                .OrderBy(name => name)
                .ToArray();

            var selectedIndex = Array.IndexOf(
                availableViewModels, 
                targetScript.ViewModelTypeName
            );

            EditorStyles.label.fontStyle = propertyPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            var newSelectedIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Template view model", 
                    "Type of the view model that this template will be bound to when it is instantiated."
                ),
                selectedIndex,
                availableViewModels
                    .Select(viewModel => new GUIContent(viewModel))
                    .ToArray()
            );

            EditorStyles.label.fontStyle = DefaultFontStyle;

            UpdateProperty(newValue => targetScript.ViewModelTypeName = newValue,
                selectedIndex < 0 
                    ? string.Empty 
                    : availableViewModels[selectedIndex],
                newSelectedIndex < 0 
                    ? string.Empty 
                    : availableViewModels[newSelectedIndex],
                "Set bound view-model for template"
            );
        }

        /// <summary>
        /// Check whether each of the properties on the object have been changed from the value in the prefab.
        /// </summary>
        private void UpdatePrefabModifiedProperties()
        {
            var property = serializedObject.GetIterator();
            // Need to call Next(true) to get the first child. Once we have it, Next(false)
            // will iterate through the properties.
            property.Next(true);
            do
            {
                if (property.name == "viewModelTypeName")
                {
                    propertyPrefabModified = property.prefabOverride;
                }
            }
            while (property.Next(false));
        }
    }
}
