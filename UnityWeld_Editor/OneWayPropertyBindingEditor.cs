using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityWeld.Binding;
using UnityWeld.Binding.Internal;

namespace UnityWeld_Editor
{
    [CustomEditor(typeof(OneWayPropertyBinding))]
    class OneWayPropertyBindingEditor : BaseBindingEditor
    {
        private OneWayPropertyBinding targetScript;

        private AnimBool viewAdapterOptionsFade;

        // Whether each property in the target differs from the prefab it uses.
        private bool viewAdapterPrefabModified;
        private bool viewAdapterOptionsPrefabModified;
        private bool viewModelPropertyPrefabModified;
        private bool viewPropertyPrefabModified;

        protected override void OnEnabled()
        {
            // Initialise reference to target script
            targetScript = (OneWayPropertyBinding)target;

            viewAdapterOptionsFade = new AnimBool(ShouldShowAdapterOptions(targetScript.ViewAdapterId, out _));
            viewAdapterOptionsFade.valueChanged.AddListener(Repaint);
        }

        private void OnDisable()
        {
            viewAdapterOptionsFade.valueChanged.RemoveListener(Repaint);
        }

        protected override void OnInspector()
        {
            UpdatePrefabModifiedProperties();

            var defaultLabelStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = viewPropertyPrefabModified 
                ? FontStyle.Bold 
                : defaultLabelStyle;

            Type viewPropertyType;
            ShowViewPropertyMenu(
                new GUIContent("View property", "Property on the view to bind to"),
                PropertyFinder.GetBindableProperties(targetScript.gameObject),
                updatedValue => targetScript.ViewPropertyName = updatedValue,
                targetScript.ViewPropertyName,
                out viewPropertyType
            );

            // Don't let the user set anything else until they've chosen a view property.
            var guiPreviouslyEnabled = GUI.enabled;
            if (string.IsNullOrEmpty(targetScript.ViewPropertyName))
            {
                GUI.enabled = false;
            }

            var viewAdapterTypeNames = TypeResolver.GetAdapterIds(
                o => viewPropertyType == null || o.OutType == viewPropertyType);

            EditorStyles.label.fontStyle = viewAdapterPrefabModified 
                ? FontStyle.Bold 
                : defaultLabelStyle;

            ShowAdapterMenu(
                new GUIContent(
                    "View adapter", 
                    "Adapter that converts values sent from the view-model to the view."
                ),
                viewAdapterTypeNames,
                targetScript.ViewAdapterId,
                newValue =>
                {
                    // Get rid of old adapter options if we changed the type of the adapter.
                    if (newValue != targetScript.ViewAdapterId)
                    {
                        Undo.RecordObject(targetScript, "Set view adapter options");
                        targetScript.ViewAdapterOptions = null;
                    }

                    UpdateProperty(
                        updatedValue => targetScript.ViewAdapterId = updatedValue,
                        targetScript.ViewAdapterId,
                        newValue,
                        "Set view adapter"
                    );
                }
            );

            Type adapterType;
            viewAdapterOptionsFade.target = ShouldShowAdapterOptions(
                targetScript.ViewAdapterId, 
                out adapterType
            );

            EditorStyles.label.fontStyle = viewAdapterOptionsPrefabModified 
                ? FontStyle.Bold 
                : defaultLabelStyle;

            ShowAdapterOptionsMenu(
                "View adapter options", 
                adapterType, 
                options => targetScript.ViewAdapterOptions = options,
                targetScript.ViewAdapterOptions,
                viewAdapterOptionsFade.faded
            );

            EditorGUILayout.Space();

            EditorStyles.label.fontStyle = viewModelPropertyPrefabModified 
                ? FontStyle.Bold 
                : defaultLabelStyle;

            var adaptedViewPropertyType = AdaptTypeBackward(
                viewPropertyType, 
                targetScript.ViewAdapterId
            );
            ShowViewModelPropertyMenu(
                new GUIContent(
                    "View-model property", 
                    "Property on the view-model to bind to."
                ),
                TypeResolver.FindBindableProperties(targetScript),
                updatedValue => targetScript.ViewModelPropertyName = updatedValue,
                targetScript.ViewModelPropertyName,
                property => property.PropertyType == adaptedViewPropertyType
            );

            GUI.enabled = guiPreviouslyEnabled;

            EditorStyles.label.fontStyle = defaultLabelStyle;
        }

        /// <summary>
        /// Check whether each of the properties on the object have been changed 
        /// from the value in the prefab.
        /// </summary>
        private void UpdatePrefabModifiedProperties()
        {
            var property = serializedObject.GetIterator();
            // Need to call Next(true) to get the first child. Once we have it, Next(false)
            // will iterate through the properties.
            property.Next(true);
            do
            {
                switch (property.name)
                {
                    case "viewAdapterTypeName":
                        viewAdapterPrefabModified = property.prefabOverride;
                        break;

                    case "viewAdapterOptions":
                        viewAdapterOptionsPrefabModified = property.prefabOverride;
                        break;

                    case "viewModelPropertyName":
                        viewModelPropertyPrefabModified = property.prefabOverride;
                        break;

                    case "viewPropertyName":
                        viewPropertyPrefabModified = property.prefabOverride;
                        break;
                }
            }
            while (property.Next(false));
        }
    }
}
