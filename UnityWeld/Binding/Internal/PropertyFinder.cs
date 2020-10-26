using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityWeld.Binding.Internal
{
    /// <summary>
    /// Helper to find bindable properties.
    /// </summary>
    public static class PropertyFinder
    {
        /// <summary>
        /// List of types to exclude from the types of components in the UI we can bind to.
        /// </summary>
        private static readonly HashSet<Type> hiddenTypes = new HashSet<Type>{
            typeof(AbstractMemberBinding),
            typeof(OneWayPropertyBinding),
            typeof(TwoWayPropertyBinding)
        };

        /// <summary>
        /// Use reflection to find all components with properties we can bind to.
        /// </summary>
        public static IEnumerable<BindableMember<PropertyInfo>> GetBindableProperties(GameObject gameObject) //todo: Maybe move this to the TypeResolver.
        {
            using(var cache = gameObject.GetComponentsWithCache<Component>())
            {
                return cache.Components
                       .SelectMany(component =>
                       {
                           var type = component.GetType();
                           return type
                                  .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  .Select(p => new BindableMember<PropertyInfo>(p, type));
                       })
                       .Where(prop => prop.Member.GetSetMethod(false) != null
                                      && prop.Member.GetGetMethod(false) != null
                                      && !hiddenTypes.Contains(prop.ViewModelType)
                                      && !prop.Member.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any()
                       );
            }
        }
    }
}
