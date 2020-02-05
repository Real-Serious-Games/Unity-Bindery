using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityWeld.Binding;
using UnityWeld.Binding.Internal;

namespace UnityWeld_Editor
{
    [CustomEditor(typeof(TwoWayPropertyBinding))]
    class TwoWayPropertyBindingEditor : BaseBindingEditor
    {
        private TwoWayPropertyBinding targetScript;

        private AnimBool viewAdapterOptionsFade;
        private AnimBool viewModelAdapterOptionsFade;
        private AnimBool exceptionAdapterOptionsFade;

        // Whether properties in the target script differ from the value in the prefab.
        // Needed to know which ones to display as bold in the inspector.
        private bool viewEventPrefabModified;
        private bool viewPropertyPrefabModified;
        private bool viewAdapterPrefabModified;
        private bool viewAdapterOptionsPrefabModified;

        private bool viewModelPropertyPrefabModified;
        private bool viewModelAdapterPrefabModified;
        private bool viewModelAdapterOptionsPrefabModified;

        private bool exceptionPropertyPrefabModified;
        private bool exceptionAdapterPrefabModified;
        private bool exceptionAdapterOptionsPrefabModified;

        protected override void OnEnabled()
        {
            targetScript = (TwoWayPropertyBinding)target;

            viewAdapterOptionsFade = new AnimBool(ShouldShowAdapterOptions(targetScript.ViewAdapterId, out _));
            viewModelAdapterOptionsFade = new AnimBool(ShouldShowAdapterOptions(targetScript.ViewModelAdapterId, out _));
            exceptionAdapterOptionsFade = new AnimBool(ShouldShowAdapterOptions(targetScript.ExceptionAdapterTypeName, out _));

            viewAdapterOptionsFade.valueChanged.AddListener(Repaint);
            viewModelAdapterOptionsFade.valueChanged.AddListener(Repaint);
            exceptionAdapterOptionsFade.valueChanged.AddListener(Repaint);
        }

        private void OnDisable()
        {
            viewAdapterOptionsFade.valueChanged.RemoveListener(Repaint);
            viewModelAdapterOptionsFade.valueChanged.RemoveListener(Repaint);
            exceptionAdapterOptionsFade.valueChanged.RemoveListener(Repaint);
        }

        protected override void OnInspector()
        {
            UpdatePrefabModifiedProperties();

            EditorStyles.label.fontStyle = viewEventPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            ShowEventMenu(
                UnityEventWatcher.GetBindableEvents(targetScript.gameObject)
                    .OrderBy(evt => evt.Name)
                    .ToArray(),
                updatedValue => targetScript.ViewEventName = updatedValue,
                targetScript.ViewEventName
            );

            EditorStyles.label.fontStyle = viewPropertyPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            Type viewPropertyType;
            ShowViewPropertyMenu(
                new GUIContent("View property", "Property on the view to bind to"),
                PropertyFinder.GetBindableProperties(targetScript.gameObject)
                    .OrderBy(prop => prop.ViewModelTypeName)
                    .ThenBy(prop => prop.MemberName)
                    .ToArray(),
                updatedValue => targetScript.ViewPropertyName = updatedValue,
                targetScript.ViewPropertyName,
                out viewPropertyType
            );

            // Don't let the user set other options until they've set the event and view property.
            var guiPreviouslyEnabled = GUI.enabled;
            if (string.IsNullOrEmpty(targetScript.ViewEventName) 
                || string.IsNullOrEmpty(targetScript.ViewPropertyName))
            {
                GUI.enabled = false;
            }

            var viewAdapterTypeNames = TypeResolver.GetAdapterIds(
                o => viewPropertyType == null ||  o.OutType == viewPropertyType);

            EditorStyles.label.fontStyle = viewAdapterPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

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

            EditorStyles.label.fontStyle = viewAdapterOptionsPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            Type viewAdapterType;
            viewAdapterOptionsFade.target = ShouldShowAdapterOptions(
                targetScript.ViewAdapterId, 
                out viewAdapterType
            );
            ShowAdapterOptionsMenu(
                "View adapter options",
                viewAdapterType,
                options => targetScript.ViewAdapterOptions = options,
                targetScript.ViewAdapterOptions,
                viewAdapterOptionsFade.faded
            );

            EditorGUILayout.Space();

            EditorStyles.label.fontStyle = viewModelPropertyPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

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
                prop => prop.PropertyType == adaptedViewPropertyType
            );

            var viewModelAdapterTypeNames = TypeResolver.GetAdapterIds(
                o => adaptedViewPropertyType == null ||  o.OutType == adaptedViewPropertyType);

            EditorStyles.label.fontStyle = viewModelAdapterPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            ShowAdapterMenu(
                new GUIContent(
                    "View-model adapter", 
                    "Adapter that converts from the view back to the view-model"
                ),
                viewModelAdapterTypeNames,
                targetScript.ViewModelAdapterId,
                newValue =>
                {
                    if (newValue != targetScript.ViewModelAdapterId)
                    {
                        Undo.RecordObject(targetScript, "Set view-model adapter options");
                        targetScript.ViewModelAdapterOptions = null;
                    }

                    UpdateProperty(
                        updatedValue => targetScript.ViewModelAdapterId = updatedValue,
                        targetScript.ViewModelAdapterId,
                        newValue,
                        "Set view-model adapter"
                    );
                }
            );

            EditorStyles.label.fontStyle = viewModelAdapterOptionsPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            Type viewModelAdapterType;
            viewModelAdapterOptionsFade.target = ShouldShowAdapterOptions(
                targetScript.ViewModelAdapterId, 
                out viewModelAdapterType
            );
            ShowAdapterOptionsMenu(
                "View-model adapter options",
                viewModelAdapterType,
                options => targetScript.ViewModelAdapterOptions = options,
                targetScript.ViewModelAdapterOptions,
                viewModelAdapterOptionsFade.faded
            );

            EditorGUILayout.Space();

            var expectionAdapterTypeNames = TypeResolver.GetAdapterIds(
                o => o.InType == typeof(Exception));

            EditorStyles.label.fontStyle = exceptionPropertyPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            var adaptedExceptionPropertyType = AdaptTypeForward(
                typeof(Exception), 
                targetScript.ExceptionAdapterTypeName
            );
            ShowViewModelPropertyMenuWithNone(
                new GUIContent(
                    "Exception property", 
                    "Property on the view-model to bind the exception to."
                ),
                TypeResolver.FindBindableProperties(targetScript),
                updatedValue => targetScript.ExceptionPropertyName = updatedValue,
                targetScript.ExceptionPropertyName,
                prop => prop.PropertyType == adaptedExceptionPropertyType
            );

            EditorStyles.label.fontStyle = exceptionAdapterPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            ShowAdapterMenu(
                new GUIContent(
                    "Exception adapter", 
                    "Adapter that handles exceptions thrown by the view-model adapter"
                ),
                expectionAdapterTypeNames,
                targetScript.ExceptionAdapterTypeName,
                newValue =>
                {
                    if (newValue != targetScript.ExceptionAdapterTypeName)
                    {
                        Undo.RecordObject(targetScript, "Set exception adapter options");
                        targetScript.ExceptionAdapterOptions = null;
                    }

                    UpdateProperty(
                        updatedValue => targetScript.ExceptionAdapterTypeName = updatedValue,
                        targetScript.ExceptionAdapterTypeName,
                        newValue,
                        "Set exception adapter"
                    );
                }
            );

            EditorStyles.label.fontStyle = exceptionAdapterOptionsPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            Type exceptionAdapterType;
            exceptionAdapterOptionsFade.target = ShouldShowAdapterOptions(
                targetScript.ExceptionAdapterTypeName, 
                out exceptionAdapterType
            );
            ShowAdapterOptionsMenu(
                "Exception adapter options",
                exceptionAdapterType,
                options => targetScript.ExceptionAdapterOptions = options,
                targetScript.ExceptionAdapterOptions,
                exceptionAdapterOptionsFade.faded
            );

            GUI.enabled = guiPreviouslyEnabled;
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
                    case "viewEventName":
                        viewEventPrefabModified = property.prefabOverride;
                        break;

                    case "viewPropertyName":
                        viewPropertyPrefabModified = property.prefabOverride;
                        break;

                    case "viewAdapterTypeName":
                        viewAdapterPrefabModified = property.prefabOverride;
                        break;

                    case "viewAdapterOptions":
                        viewAdapterOptionsPrefabModified = property.prefabOverride;
                        break;

                    case "viewModelPropertyName":
                        viewModelPropertyPrefabModified = property.prefabOverride;
                        break;

                    case "viewModelAdapterTypeName":
                        viewModelAdapterPrefabModified = property.prefabOverride;
                        break;

                    case "viewModelAdapterOptions":
                        viewModelAdapterOptionsPrefabModified = property.prefabOverride;
                        break;

                    case "exceptionPropertyName":
                        exceptionPropertyPrefabModified = property.prefabOverride;
                        break;

                    case "exceptionAdapterTypeName":
                        exceptionAdapterPrefabModified = property.prefabOverride;
                        break;

                    case "exceptionAdapterOptions":
                        exceptionAdapterOptionsPrefabModified = property.prefabOverride;
                        break;
                }
            }
            while (property.Next(false));
        }
    }
}
