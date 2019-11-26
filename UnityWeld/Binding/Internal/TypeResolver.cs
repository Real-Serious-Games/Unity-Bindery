using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityWeld.Binding.Adapters;
using UnityWeld.Binding.Exceptions;

namespace UnityWeld.Binding.Internal
{
    /// <summary>
    /// ViewModel provider delegate
    /// </summary>
    /// <param name="component"></param>
    /// <returns>Tuple with ViewModel name and ViewModel itself</returns>
    public delegate ViewModelProviderData ViewModelProviderDelegate(Component component);

    /// <summary>
    /// Contains ViewModel data
    /// </summary>
    public class ViewModelProviderData 
    {
        public readonly object Model;
        public readonly string TypeName;

        public ViewModelProviderData(object model, string typeName)
        {
            Model = model;
            TypeName = typeName;
        }
    }

    /// <summary>
    /// Helper class for setting up the factory for use in the editor.
    /// </summary>
    public static class TypeResolver
    {
        private static readonly IList<Type> BindingAttributeTypes = new List<Type>(2)
        {
            typeof(BindingAttribute) //this should be the only ref to default binding attribute
        };

        private static readonly IList<ViewModelProviderDelegate> ViewModelProviders = new List<ViewModelProviderDelegate>(2)
        {
            DefaultViewModelProvider
        };

        private static Type[] typesWithBindingAttribute;

        /// <summary>
        /// Find all types with the binding attribute. This uses reflection to find all
        /// types the first time it runs and caches it for every other time. We can
        /// safely cache this data because it will only change if the loaded assemblies
        /// change, in which case everthing in managed memory will be throw out anyway.
        /// </summary>
        public static IEnumerable<Type> TypesWithBindingAttribute
        {
            get
            {
                if (typesWithBindingAttribute == null)
                {
                    typesWithBindingAttribute = FindTypesMarkedByBindingAttribute();
                }

                return typesWithBindingAttribute;
            }
        }

        /// <summary>
        /// Impliments default view model provider
        /// </summary>
        private static ViewModelProviderData DefaultViewModelProvider(Component component)
        {
            var provider = component as IViewModelProvider;
            if (provider == null)
            {
                return null;
            }

            return new ViewModelProviderData(provider.GetViewModel(), provider.GetViewModelTypeName());
        }

        /// <summary>
        /// Find all types marked with the specified attribute.
        /// </summary>
        private static Type[] FindTypesMarkedByAttribute(Type attributeType)
        {
            var typesFound = new List<Type>();

            foreach (var type in GetAllTypes())
            {
                try
                {
                    if (type.GetCustomAttributes(attributeType, false).Any())
                    {
                        typesFound.Add(type);
                    }
                }
                catch (Exception)
                {
                    // Some types throw an exception when we try to use reflection on them.
                }
            }

            return typesFound.ToArray();
        }

        /// <summary>
        /// Find all types marked with binding attribute
        /// </summary>
        /// <returns></returns>
        private static Type[] FindTypesMarkedByBindingAttribute()
        {
            var result = new List<Type>();
            foreach (var attributeType in BindingAttributeTypes)
            {
                result.AddRange(FindTypesMarkedByAttribute(attributeType));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns an enumerable of all known types.
        /// </summary>
        private static IEnumerable<Type> GetAllTypes()
        {
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    // Automatically exclude the Unity assemblies, which throw exceptions when we try to access them.
                    .Where(a =>
                        !a.FullName.StartsWith("UnityEngine") &&
                        !a.FullName.StartsWith("UnityEditor"));

            foreach (var assembly in assemblies)
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    // Ignore assemblies that can't be loaded.
                    continue;
                }

                foreach (var type in types)
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Return the type of a view model bound by an IViewModelBinding
        /// </summary>
        private static Type GetViewModelType(string viewModelTypeName)
        {
            var type = TypesWithBindingAttribute
                .FirstOrDefault(t => t.ToString() == viewModelTypeName);

            if (type == null)
            {
                throw new ViewModelNotFoundException("Could not find the specified view model \"" + viewModelTypeName + "\"");
            }

            return type;
        }

        /// <summary>
        /// Scan up the hierarchy and find all the types that can be bound to 
        /// a specified MemberBinding.
        /// </summary>
        private static IEnumerable<Type> FindAvailableViewModelTypes(AbstractMemberBinding memberBinding)
        {
            var foundAtLeastOneBinding = false;

            var trans = memberBinding.transform;
            while (trans != null)
            {
                var components = trans.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    // Can't bind to self or null
                    if (component == null || component == memberBinding)
                    {
                        continue;
                    }

                    // Case where a ViewModelBinding is used to bind a non-MonoBehaviour class.
                    var viewModelData = component.GetViewModelData();
                    if (viewModelData != null)
                    {
                        // Ignore view model bindings that haven't been set up yet.
                        if (string.IsNullOrEmpty(viewModelData.TypeName))
                            continue;

                        foundAtLeastOneBinding = true;

                        yield return GetViewModelType(viewModelData.TypeName);
                    }
                    else if (component.GetType().HasBindingAttribute())
                    {
                        // Case where we are binding to an existing MonoBehaviour.
                        foundAtLeastOneBinding = true;

                        yield return component.GetType();
                    }
                }

                trans = trans.parent;
            }

            if (!foundAtLeastOneBinding)
            {
                Debug.LogError("UI binding " + memberBinding.gameObject.name + " must be placed underneath at least one bindable component.", memberBinding);
            }
        }

        /// <summary>
        /// Find bindable properties in available view models.
        /// </summary>
        public static BindableMember<PropertyInfo>[] FindBindableProperties(AbstractMemberBinding target)
        {
            return FindAvailableViewModelTypes(target)
                .SelectMany(type => GetPublicProperties(type)
                    .Select(p => new BindableMember<PropertyInfo>(p, type))
                )
                .Where(p => p.Member.HasBindingAttribute() // Filter out properties that don't have [Binding].
                )
                .ToArray();
        }

        /// <summary>
        /// Get all the declared and inherited public properties from a class or interface.
        ///
        /// https://stackoverflow.com/questions/358835/getproperties-to-return-all-properties-for-an-interface-inheritance-hierarchy#answer-26766221
        /// </summary>
        private static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
        {
            if (!type.IsInterface)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            }

            return (new[] { type })
                .Concat(type.GetInterfaces())
                .SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        /// Get all the declared and inherited public methods from a class or interface.
        /// </summary>
        private static IEnumerable<MethodInfo> GetPublicMethods(Type type)
        {
            if (!type.IsInterface)
            {
                return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            }

            return (new[] { type })
                .Concat(type.GetInterfaces())
                .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        /// Get a list of methods in the view model that we can bind to.
        /// </summary>
        public static BindableMember<MethodInfo>[] FindBindableMethods(EventBinding targetScript)
        {
            return FindAvailableViewModelTypes(targetScript)
                .SelectMany(type => GetPublicMethods(type)
                    .Select(m => new BindableMember<MethodInfo>(m, type))
                )
                .Where(m => m.Member.GetParameters().Length == 0)
                .Where(m => m.Member.HasBindingAttribute() 
                    && !m.MemberName.StartsWith("get_")) // Exclude property getters, since we aren't doing anything with the return value of the bound method anyway.
                .ToArray();
        }

        /// <summary>
        /// Find collection properties that can be data-bound.
        /// </summary>
        public static BindableMember<PropertyInfo>[] FindBindableCollectionProperties(CollectionBinding target)
        {
            return FindBindableProperties(target)
                .Where(p => typeof(IEnumerable).IsAssignableFrom(p.Member.PropertyType))
                .Where(p => !typeof(string).IsAssignableFrom(p.Member.PropertyType))
                .ToArray();
        }

        /// <summary>
        /// Register custom binding attribute.
        /// This will allow to mark bindable properties in external dlls without referencing UnityWeld.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RegisterCustomBindingAttribute<T>() where T : Attribute
        {
            var type = typeof(T);
            if (BindingAttributeTypes.Contains(type))
            {
                return;
            }

            BindingAttributeTypes.Add(type);
        }

        /// <summary>
        /// Register custom ViewModel provider.
        /// This will allow to use custom ViewModel providers in external dlls without referencing UnityWeld.
        /// </summary>
        /// <param name="provider"></param>
        public static void RegisterCustomViewModelProvider(ViewModelProviderDelegate provider)
        {
            if (ViewModelProviders.Contains(provider))
            {
                return;
            }

            ViewModelProviders.Add(provider);
        }

        /// <summary>
        /// Check if type has binding attribute
        /// </summary>
        public static bool HasBindingAttribute(this MemberInfo type)
        {
            //for to avoid GC
            for (var index = 0; index < BindingAttributeTypes.Count; index++)
            {
                if (type.GetCustomAttributes(BindingAttributeTypes[index], false).Any())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get ViewModel data from component
        /// </summary>
        public static ViewModelProviderData GetViewModelData(this Component component)
        {
            if (component == null)
                return null;

            //for to avoid GC
            for (var i = 0; i < ViewModelProviders.Count; i++)
            {
                var data = ViewModelProviders[i](component);

                if (data != null)
                    return data;
            }

            return null;
        }

        private static readonly Dictionary<string, IAdapterInfo> Adapters;

        public static IAdapterInfo GetAdapter(string adapterId)
        {
            if (string.IsNullOrEmpty(adapterId))
            {
                throw new Exception("Adapter id is empty");
            }

            if (!Adapters.TryGetValue(adapterId, out var adapter))
            {
                throw new Exception($"Adapter {adapterId} was not found!");
            }

            return adapter;
        }

        public static bool TryGetAdapter(string adapterId, out IAdapterInfo adapterInfo)
        {
            if (string.IsNullOrEmpty(adapterId))
            {
                adapterInfo = null;
                return false;
            }

            return Adapters.TryGetValue(adapterId, out adapterInfo);
        }

        public static string[] GetAdapterIds(Predicate<IAdapterInfo> predicate)
        {
            return Adapters.Values.Where(o => predicate(o)).Select(o => o.Id).ToArray();
        }

        public static void RegisterAdapter<TIn, TOut>(AdapterInfo<TIn, TOut> adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            if (Adapters.ContainsKey(adapter.Id))
            {
                throw new Exception(string.Format("Adapter with id \"{0}\"", adapter.Id));
            }

            Adapters.Add(adapter.Id, adapter);
        }
        
        public static void RegisterAdapter<TIn, TOut, TOptions>(AdapterInfo<TIn, TOut, TOptions> adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            if (Adapters.ContainsKey(adapter.Id))
            {
                throw new Exception(string.Format("Adapter with id \"{0}\"", adapter.Id));
            }

            Adapters.Add(adapter.Id, adapter);
        }

        static TypeResolver()
        {
            Adapters = new Dictionary<string, IAdapterInfo>();
            BaseAdapters.RegisterBaseAdapters();
        }
    }
}
