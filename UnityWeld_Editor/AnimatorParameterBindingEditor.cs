using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityWeld.Binding;
using UnityWeld.Binding.Internal;

namespace UnityWeld_Editor
{
    [CustomEditor(typeof(AnimatorParameterBinding))]
    public class AnimatorParameterBindingEditor : BaseBindingEditor
    {
        private AnimatorParameterBinding targetScript;

        private AnimBool viewAdapterOptionsFade;

        // Whether each property in the target differs from the prefab it uses.
        private bool viewAdapterPrefabModified;
        private bool viewAdapterOptionsPrefabModified;
        private bool viewModelPropertyPrefabModified;
        private bool viewPropertyPrefabModified;

        protected override void OnEnabled()
        {
            // Initialise reference to target script
            targetScript = (AnimatorParameterBinding)target;

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

            EditorStyles.label.fontStyle = viewPropertyPrefabModified ? FontStyle.Bold : DefaultFontStyle;

            var animatorParameters = GetAnimatorParameters();

            if (animatorParameters == null || !animatorParameters.Any())
            {
                EditorGUILayout.HelpBox("Animator has no parameters!", MessageType.Warning);
                return;
            }

            ShowAnimatorParametersMenu(
                new GUIContent("View property", "Property on the view to bind to"),
                updatedValue =>
                {
                    targetScript.AnimatorParameterName = updatedValue.Name;
                    targetScript.AnimatorParameterType = updatedValue.Type;
                },
                new AnimatorParameterTypeAndName(targetScript.AnimatorParameterName, targetScript.AnimatorParameterType),
                animatorParameters,
                out var viewPropertyType
                );

            // Don't let the user set anything else until they've chosen a view property.
            var guiPreviouslyEnabled = GUI.enabled;
            if (string.IsNullOrEmpty(targetScript.AnimatorParameterName))
            {
                GUI.enabled = false;
            }

            var viewAdapterTypeNames = TypeResolver.GetAdapterIds(
                adapterInfo => viewPropertyType == null || adapterInfo.OutType == viewPropertyType);

            EditorStyles.label.fontStyle = viewAdapterPrefabModified ? FontStyle.Bold : DefaultFontStyle;

            ShowAdapterMenu(
                new GUIContent("View adapter", "Adapter that converts values sent from the view-model to the view."),
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

            viewAdapterOptionsFade.target = ShouldShowAdapterOptions(targetScript.ViewAdapterId, out var adapterType);

            EditorStyles.label.fontStyle = viewAdapterOptionsPrefabModified ? FontStyle.Bold : DefaultFontStyle;

            ShowAdapterOptionsMenu(
                "View adapter options",
                adapterType,
                options => targetScript.ViewAdapterOptions = options,
                targetScript.ViewAdapterOptions,
                viewAdapterOptionsFade.faded
            );

            EditorGUILayout.Space();

            EditorStyles.label.fontStyle = viewModelPropertyPrefabModified ? FontStyle.Bold : DefaultFontStyle;

            var adaptedViewPropertyType = AdaptTypeBackward(viewPropertyType, targetScript.ViewAdapterId);
            ShowViewModelPropertyMenu(
                new GUIContent("View-model property", "Property on the view-model to bind to."),
                TypeResolver.FindBindableProperties(targetScript),
                updatedValue => targetScript.ViewModelPropertyName = updatedValue,
                targetScript.ViewModelPropertyName,
                property => property.PropertyType == adaptedViewPropertyType
            );

            GUI.enabled = guiPreviouslyEnabled;

            EditorGUILayout.Space();

        }

        private void ShowAnimatorParametersMenu(
            GUIContent label,
            Action<AnimatorParameterTypeAndName> propertyValueSetter,
            AnimatorParameterTypeAndName curPropertyValue,
            AnimatorControllerParameter[] properties,
            out Type selectedPropertyType
        )
        {
            if(properties == null || !properties.Any())
            {
                selectedPropertyType = null;
                return;
            }

            var propertyNamesAndTypes = properties
                .Select(m => new AnimatorParameterTypeAndName(m.name, m.type))
                .ToArray();
            var selectedIndex = Array.IndexOf(propertyNamesAndTypes, curPropertyValue);
            var content = properties.Select(prop => new GUIContent(string.Concat(
                    prop.name,
                    " : ",
                    prop.type.ToString()
                )))
                .ToArray();

            var newSelectedIndex = EditorGUILayout.Popup(label, selectedIndex, content);
            if (newSelectedIndex != selectedIndex)
            {
                var newSelectedProperty = properties[newSelectedIndex];

                UpdateProperty(
                    propertyValueSetter,
                    curPropertyValue,
                    new AnimatorParameterTypeAndName(newSelectedProperty.name, newSelectedProperty.type),
                    "Set Animator parameter"
                );

                selectedPropertyType = AnimatorControllerParameterTypeToType(newSelectedProperty.type);
            }
            else
            {
                if (selectedIndex < 0)
                {
                    selectedPropertyType = null;
                    return;
                }

                selectedPropertyType = AnimatorControllerParameterTypeToType(properties[selectedIndex].type);
            }
        }

        /// <summary>
        /// Returns the corresponding System.Type from AnimatorControllerParameterType
        /// </summary>
        /// <param name="parameterType">The type of parameter</param>
        /// <returns>The System.Type corresponding to paramter type</returns>
        private static Type AnimatorControllerParameterTypeToType(
            AnimatorControllerParameterType parameterType
        )
        {
            switch (parameterType)
            {
                case AnimatorControllerParameterType.Bool:
                    return typeof(bool);
                case AnimatorControllerParameterType.Float:
                    return typeof(float);
                case AnimatorControllerParameterType.Int:
                    return typeof(int);
                case AnimatorControllerParameterType.Trigger:
                    return typeof(AnimatorParameterTrigger);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets the Animator component on the targetScript and returns it's parameters
        /// </summary>
        /// <returns>An array of the Animator components parameters as UnityEngine.AnimatorControllerParameter</returns>
        private AnimatorControllerParameter[] GetAnimatorParameters()
        {
            var animator = targetScript.GetComponent<Animator>();

            AnimatorControllerParameter[] properties;

            if(animator.runtimeAnimatorController == null)
            {
                return null;
            }

            // For whatever reason, accessing Animator.parameters while the GameObject the
            // Animator is on is inactive causes it to return an empty array, aswell as fill
            // the unity console with warnings. This behaviour is undocumented, and this is just
            // a work around which seems to work fine
            if (!animator.gameObject.activeInHierarchy)
            {
                var tPos = animator.transform.position;
                var tScale = animator.transform.localScale;
                var tRotation = animator.transform.rotation;

                var parent = animator.transform.parent;
                var siblingIndex = animator.transform.GetSiblingIndex();
                var isActive = animator.gameObject.activeSelf;
                animator.transform.SetParent(null);
                animator.gameObject.SetActive(true);
                properties = animator.parameters;
                animator.transform.SetParent(parent);
                animator.transform.SetSiblingIndex(siblingIndex);
                animator.gameObject.SetActive(isActive);

                animator.transform.position = tPos;
                animator.transform.localScale = tScale;
                animator.transform.rotation = tRotation;
            }
            else
            {
                properties = animator.parameters;
                //Another odd fix to refresh the parameters
                //When the animator is active, & you add a trigger, or modify it's triggers, the parameters property
                //on the animator comes back empty, toggling it unactive, then back seems to fix the list of parameters returned.
                if (properties == null || properties.Length == 0)
                {
                    var isActive = animator.gameObject.activeSelf;
                    animator.gameObject.SetActive(false);
                    animator.gameObject.SetActive(true);
                    animator.gameObject.SetActive(isActive);
                    properties = animator.parameters;
                }
            }

            return properties;
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
                    case "viewAdapterTypeName":
                        viewAdapterPrefabModified = property.prefabOverride;
                        break;

                    case "viewAdapterOptions":
                        viewAdapterOptionsPrefabModified = property.prefabOverride;
                        break;

                    case "viewModelPropertyName":
                        viewModelPropertyPrefabModified = property.prefabOverride;
                        break;

                    case "animatorParameterType":
                    case "animatorParameterName":
                        viewPropertyPrefabModified = property.prefabOverride;
                        break;
                }
            }
            while (property.Next(false));
        }

        private class AnimatorParameterTypeAndName : IEquatable<AnimatorParameterTypeAndName>
        {
            public string Name { get; private set; }
            public AnimatorControllerParameterType Type { get; private set; }
            public AnimatorParameterTypeAndName(string name, AnimatorControllerParameterType type)
            {
                Name = name;
                Type = type;
            }

            public override int GetHashCode()
            {
                return new { Name, Type }.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var objAsThis = obj as AnimatorParameterTypeAndName;
                if (objAsThis != null)
                {
                    return Equals(objAsThis);
                }
                return false;
            }

            public bool Equals(AnimatorParameterTypeAndName other)
            {
                return other != null && other.Name == Name && other.Type == Type;
            }
        }
    }
}
