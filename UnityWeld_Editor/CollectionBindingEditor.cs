using UnityEditor;
using UnityEngine;
using UnityWeld.Binding;
using UnityWeld.Binding.Internal;

namespace UnityWeld_Editor
{
    [CustomEditor(typeof(CollectionBinding))]
    class CollectionBindingEditor : BaseBindingEditor
    {
        private CollectionBinding _targetScript;
        private SerializedProperty _templateInitialPoolCountProperty;
        private SerializedProperty _itemsContainerProperty;
        private SerializedProperty _templatesProperty;
        
        private bool _viewModelPrefabModified;

        protected override void OnEnabled()
        {
            // Initialise everything
            _targetScript = (CollectionBinding)target;
            _templateInitialPoolCountProperty = serializedObject.FindProperty("_templateInitialPoolCount");
            _itemsContainerProperty = serializedObject.FindProperty("_itemsContainer");
            _templatesProperty = serializedObject.FindProperty("_templates");
        }

        protected override void OnInspector()
        {
            UpdatePrefabModifiedProperties();

            EditorGUILayout.PropertyField(_templateInitialPoolCountProperty);
            EditorGUILayout.PropertyField(_itemsContainerProperty);
            EditorGUILayout.PropertyField(_templatesProperty, true);
            
            EditorStyles.label.fontStyle = _viewModelPrefabModified ? FontStyle.Bold : DefaultFontStyle;
            ShowViewModelPropertyMenu(
                new GUIContent("View-model property", "Property on the view-model to bind to."),
                TypeResolver.FindBindableCollectionProperties(_targetScript),
                updatedValue => _targetScript.ViewModelPropertyName = updatedValue,
                _targetScript.ViewModelPropertyName,
                property => true
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
                switch (property.name)
                {
                    case "viewModelPropertyName":
                        _viewModelPrefabModified = property.prefabOverride;
                        break;
                }
            }
            while (property.Next(false));
        }
    }
}
