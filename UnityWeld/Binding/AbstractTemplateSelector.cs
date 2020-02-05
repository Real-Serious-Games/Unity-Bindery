using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityWeld.Binding.Exceptions;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    public abstract class AbstractTemplateSelector : AbstractMemberBinding
    {
        [SerializeField] private GameObject templatesRoot;
        [SerializeField] private string viewModelPropertyName = string.Empty;

        private IDictionary<Type, Template> _availableTemplates;

        /// <summary>
        /// All the child objects that have been created, indexed by the view they are connected to.
        /// </summary>
        private readonly IDictionary<object, Template> _instantiatedTemplates = new Dictionary<object, Template>();


        /// <summary>
        /// The view-model, cached during connection.
        /// </summary>
        protected object ViewModel;

        /// <summary>
        /// Watches the view-model property for changes.
        /// </summary>
        protected PropertyWatcher ViewModelPropertyWatcher;

        /// <summary>
        /// The name of the property we are binding to on the view model.
        /// </summary>
        public string ViewModelPropertyName
        {
            get => viewModelPropertyName;
            set => viewModelPropertyName = value;
        }

        /// <summary>
        /// The GameObject in the scene that is the parent object for the tenplates.
        /// </summary>
        public GameObject TemplatesRoot
        {
            get => templatesRoot;
            set => templatesRoot = value;
        }

        /// <summary>
        /// All available templates indexed by the view model the are for.
        /// </summary>
        protected IDictionary<Type, Template> AvailableTemplates
        {
            get
            {
                if(_availableTemplates == null)
                {
                    CacheTemplates();
                }

                return _availableTemplates;
            }
        }

        // Cache available templates.
        private void CacheTemplates()
        {
            _availableTemplates = new Dictionary<Type, Template>();

            var buffer = Buffer.Templates;
            templatesRoot.GetComponentsInChildren(true, buffer);
            foreach(var template in buffer)
            {
                template.gameObject.SetActive(false);
                var typeName = template.GetViewModelTypeName();
                var type = TypeResolver.TypesWithBindingAttribute.FirstOrDefault(t => t.ToString() == typeName);
                if(type == null)
                {
                    Debug.LogError(
                        $"Template object {template.name} references type {typeName}, but no matching type with a [Binding] attribute could be found.",
                        template);
                    continue;
                }

                _availableTemplates.Add(type, template);
            }
        }

        /// <summary>
        /// Returns the template that best matches the specified type.
        /// </summary>
        private Template FindTemplateForType(Type templateType)
        {
            var possibleMatches = FindTypesMatchingTemplate(templateType);
            // .OrderBy(m => m.Key)
            // .ToList();

            if(possibleMatches.Count == 0)
            {
                throw new TemplateNotFoundException("Could not find any template matching type " + templateType);
            }

            if(possibleMatches.Count > 1)
            {
                throw new AmbiguousTypeException("Multiple templates were found that match type " + templateType
                                                                                                  + ". This can be caused by providing multiple templates that match types " +
                                                                                                  templateType
                                                                                                  + " inherits from the same level. Remove one or provide a template that more specifically matches the type.");
            }

            return AvailableTemplates[possibleMatches[0]];
        }

        private static List<Type> GetTypeWithInterfaces(Type originalType)
        {
            var interfaces = originalType.GetInterfaces();
            var result = new List<Type>(interfaces.Length + 1)
            {
                originalType
            };
            result.AddRange(interfaces);
            return result;
        }

        private static List<Type> GetBaseTypeWithInterfaces(Type originalType)
        {
            var interfaces = originalType.GetInterfaces();
            var result = new List<Type>(interfaces.Length + 1);
            if(originalType.BaseType != null)
            {
                result.Add(originalType.BaseType);
            }

            result.AddRange(interfaces);
            return result;
        }

        private List<Type> FindTypesMatchingTemplate(Type originalType)
        {
            var result = new List<Type>();
            var levelToCheck = GetTypeWithInterfaces(originalType);

            while(levelToCheck.Count > 0)
            {
                var validTypesList = new List<Type>();
                var newLevelToCheck = new List<Type>();
                foreach(var type in levelToCheck)
                {
                    newLevelToCheck.AddRange(GetBaseTypeWithInterfaces(type));

                    if(AvailableTemplates.ContainsKey(type))
                    {
                        validTypesList.Add(type);
                    }
                }

                if(validTypesList.Count > 0)
                {
                    return validTypesList;
                }

                if(newLevelToCheck.Count > 0)
                {
                    levelToCheck = newLevelToCheck;
                }
            }

            return result;
        }

        protected virtual Template CloneTemplate(Template template)
        {
            return Instantiate(template, transform);
        }

        protected virtual void OnTemplateDestroy(Template template) { }

        /// <summary>
        /// Create a clone of the template object and bind it to the specified view model.
        /// Place the new object under the parent at the specified index, or 0 if no index
        /// is specified.
        /// </summary>
        protected void InstantiateTemplate(object templateViewModel, int index = 0)
        {
            Assert.IsNotNull(templateViewModel, "Cannot instantiate child with null view model");

            // Select template.
            var selectedTemplate = FindTemplateForType(templateViewModel.GetType());
            var newObject = CloneTemplate(selectedTemplate);

            newObject.transform.SetSiblingIndex(index);
            _instantiatedTemplates.Add(templateViewModel, newObject);

            // Set up child bindings before we activate the template object so that they will be configured properly before trying to connect.
            newObject.InitChildBindings(templateViewModel);
            newObject.gameObject.SetActive(true);
        }

        /// <summary>
        /// Recursively look in the type, interfaces it implements and types it inherits
        /// from for a type that matches a template. Also store how many steps away from 
        /// the specified template the found template was.
        /// </summary>
        // private IEnumerable<KeyValuePair<int, Type>> FindTypesMatchingTemplate(Type t, int index = 0)
        // {
        //     var baseType = t.BaseType;
        //     if (baseType != null && !baseType.IsInterface)
        //     {
        //         foreach (var type in FindTypesMatchingTemplate(baseType, index + 1))
        //         {
        //             yield return type;
        //         }
        //     }
        //
        //     foreach (var interfaceType in t.GetInterfaces())
        //     {
        //         foreach (var type in FindTypesMatchingTemplate(interfaceType, index + 1))
        //         {
        //             yield return type;
        //         }
        //     }
        //
        //     if (AvailableTemplates.Keys.Contains(t))
        //     {
        //         yield return new KeyValuePair<int, Type>(index, t);
        //     }
        // }

        /// <summary>
        /// Destroys the instantiated template associated with the provided object.
        /// </summary>
        protected void DestroyTemplate(object viewModelToDestroy)
        {
            var template = _instantiatedTemplates[viewModelToDestroy];
            OnTemplateDestroy(template);
            _instantiatedTemplates.Remove(viewModelToDestroy);
        }

        /// <summary>
        /// Destroys all instantiated templates.
        /// </summary>
        protected void DestroyAllTemplates()
        {
            var templates = _instantiatedTemplates.Values.ToList();
            _instantiatedTemplates.Clear();

            foreach(var template in templates)
            {
                OnTemplateDestroy(template);
            }
        }
    }
}