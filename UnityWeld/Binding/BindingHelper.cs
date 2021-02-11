using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityWeld.Binding
{
    public interface IComponentsCache : IDisposable{}

    public interface IComponentsCache<T> : IComponentsCache
    {
        IList<T> Components { get; }
    }

    public static class BindingHelper
    {
        private class ComponentsCache<T> : IComponentsCache<T>
        {
            public IList<T> Components { get; }
            public Type Type { get; }

            public ComponentsCache(IList<T> cache)
            {
                Components = cache;
                Type = typeof(T);
            }

            public void Dispose()
            {
                Components.Clear();
                InternalCache[Type].Enqueue((IList)Components);
            }
        }

        private static readonly Dictionary<Type, Queue<IList>> InternalCache = new Dictionary<Type, Queue<IList>>();

        public static IComponentsCache<T> GetComponentsWithCache<T>(this GameObject gameObject, bool withChildren = true)
            where T : Component
        {
            if(gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            var type = typeof(T);

            List<T> cache;
            if(!InternalCache.TryGetValue(type, out var cacheQueue))
            {
                InternalCache.Add(type, cacheQueue = new Queue<IList>());
            }

            if(cacheQueue.Count == 0)
            {
                cache = new List<T>(100);
            }
            else
            {
                cache = (List<T>)cacheQueue.Dequeue();
                cache.Clear();
            }

            if(withChildren)
            {
                gameObject.GetComponentsInChildren(cache);
            }
            else
            {
                gameObject.GetComponents(cache);
            }

            return new ComponentsCache<T>(cache);
        }
    }
}