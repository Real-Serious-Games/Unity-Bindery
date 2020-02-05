using UnityEditor;
using UnityEngine;
using UnityWeld.Binding;
using UnityWeld.Binding.Internal;

namespace UnityWeld_Editor
{
    [CustomEditor(typeof(TemplateBinding))]
    class TemplateBindingEditor : BaseBindingEditor
    {
        private TemplateBinding targetScript;

        private bool viewModelPrefabModified;
        private bool templatesRootPrefabModified;

        protected override  void OnEnabled()
        {
            targetScript = (TemplateBinding)target;
        }

        protected override void OnInspector()
        {
            UpdatePrefabModifiedProperties();

            EditorStyles.label.fontStyle = viewModelPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            ShowViewModelPropertyMenu(
                new GUIContent(
                    "Template property", 
                    "Property on the view model to use for selecting templates."
                ),
                TypeResolver.FindBindableProperties(targetScript),
                updatedValue => targetScript.ViewModelPropertyName = updatedValue,
                targetScript.ViewModelPropertyName,
                property => true
            );

            EditorStyles.label.fontStyle = templatesRootPrefabModified 
                ? FontStyle.Bold 
                : DefaultFontStyle;

            UpdateProperty(
                updatedValue => targetScript.TemplatesRoot = updatedValue,
                targetScript.TemplatesRoot,
                (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Templates root object", 
                        "Parent object to the objects we want to use as templates."
                    ),
                    targetScript.TemplatesRoot, 
                    typeof(GameObject), 
                    true
                ),
                "Set template binding root object"
            );
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
                    case "viewModelPropertyName":
                        viewModelPrefabModified = property.prefabOverride;
                        break;

                    case "templatesRoot":
                        templatesRootPrefabModified = property.prefabOverride;
                        break;
                }
            }
            while (property.Next(false));
        }
    }
}
