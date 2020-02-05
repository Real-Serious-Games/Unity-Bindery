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
        
        private bool _viewModelPrefabModified;
        private bool _templatesRootPrefabModified;
        private bool _templateInitialPoolCountPrefabModified;
        private bool _itemsContainerPrefabModified;

        protected override void OnEnabled()
        {
            // Initialise everything
            _targetScript = (CollectionBinding)target;
            _templateInitialPoolCountProperty = serializedObject.FindProperty("_templateInitialPoolCount");
            _itemsContainerProperty = serializedObject.FindProperty("_itemsContainer");
        }

        protected override void OnInspector()
        {
            UpdatePrefabModifiedProperties();

            EditorStyles.label.fontStyle = _templateInitialPoolCountPrefabModified ? FontStyle.Bold : DefaultFontStyle;
            EditorGUILayout.PropertyField(_templateInitialPoolCountProperty);

            EditorStyles.label.fontStyle = _itemsContainerPrefabModified ? FontStyle.Bold : DefaultFontStyle;
            EditorGUILayout.PropertyField(_itemsContainerProperty);
            
            EditorStyles.label.fontStyle = _viewModelPrefabModified ? FontStyle.Bold : DefaultFontStyle;
            ShowViewModelPropertyMenu(
                new GUIContent("View-model property", "Property on the view-model to bind to."),
                TypeResolver.FindBindableCollectionProperties(_targetScript),
                updatedValue => _targetScript.ViewModelPropertyName = updatedValue,
                _targetScript.ViewModelPropertyName,
                property => true
            );

            EditorStyles.label.fontStyle = _templatesRootPrefabModified ? FontStyle.Bold : DefaultFontStyle;
            UpdateProperty(
                updatedValue => _targetScript.TemplatesRoot = updatedValue,
                _targetScript.TemplatesRoot,
                (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Collection templates", "Parent object for all templates to copy and bind to items in the collection."), 
                    _targetScript.TemplatesRoot, 
                    typeof(GameObject), 
                    true
                ),
                "Set collection templates root"
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

                    case "TemplateInitialPoolCount":
                        _templateInitialPoolCountPrefabModified = property.prefabOverride;
                        break;

                    case "templatesRoot":
                        _templatesRootPrefabModified = property.prefabOverride;
                        break;
                    
                    case "_itemsContainer":
                        _itemsContainerPrefabModified = property.prefabOverride;
                        break;
                }
            }
            while (property.Next(false));
        }
    }
}
