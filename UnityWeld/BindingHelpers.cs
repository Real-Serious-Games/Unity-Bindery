using System;
using System.Collections.Generic;
using UnityEngine;
using UnityWeld.Binding;

namespace UnityWeld
{
    public static class BindingHelpers
    {
        /// <summary>
        /// Iterate though all child with specified component except objects with name <see cref="Template.TemplatesContainerName"/>
        /// </summary>
        /// <param name="view">start object</param>
        /// <param name="skipCurrent">dont check start object</param>
        /// <typeparam name="T">specified component</typeparam>
        /// <returns>component enumerable</returns>
        public static IEnumerable<T> IterateComponents<T>(GameObject view, bool skipCurrent = false) where T : Component
        {
            var stack = new Stack<Transform>();

            if(skipCurrent)
            {
                if(view.transform.childCount == 0)
                {
                    yield break;
                }

                foreach(Transform child in view.transform)
                {
                    stack.Push(child);
                }
            }
            else
            {
                stack.Push(view.transform);
            }

            while (stack.Count > 0)
            {
                var transform = stack.Pop();

                var component = transform.GetComponent<T>();
                if (component != null)
                {
                    yield return component;
                }

                foreach (Transform child in transform)
                {
                    stack.Push(child);
                }
            }
        }
    }
}