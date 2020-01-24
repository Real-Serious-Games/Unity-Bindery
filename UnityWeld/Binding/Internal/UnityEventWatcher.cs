using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityWeld.Binding.Exceptions;

namespace UnityWeld.Binding.Internal
{
    /// <summary>
    /// Information needed to bind to a UnityEvent on a component.
    /// </summary>
    public class BindableEvent
    {
        /// <summary>
        /// UnityEvent to bind to.
        /// </summary>
        public UnityEventBase UnityEvent { get; set; }

        /// <summary>
        /// The name of the property or field contaning this event.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type that the event belongs to.
        /// </summary>
        public Type DeclaringType { get; set; }

        /// <summary>
        /// Type of the component we're binding to. This doesn't account for multiple 
        /// components of the same type on the same GameObject, but there doesn't seem
        /// to be any other easy way of doing that since component instance IDs are
        /// re-generated frequently.
        /// </summary>
        public Type ComponentType { get; set; }
    }

    /// <summary>
    /// Watches an component for a Unity event and triggers an action when the event is raised.
    /// </summary>
    public class UnityEventWatcher : IDisposable
    {
        /// <summary>
        /// Helper object that links to the Unity event.
        /// </summary>
        private UnityEventBinderBase unityEventBinder;

        private bool disposed;

        public UnityEventWatcher(Component component, string eventName, Action action)
        {
            Assert.IsNotNull(component);
            Assert.IsFalse(string.IsNullOrEmpty(eventName));
            Assert.IsNotNull(action);

            unityEventBinder = UnityEventBinderFactory.Create(GetBoundEvent(eventName, component).UnityEvent, action);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing && unityEventBinder != null)
            {
                unityEventBinder.Dispose();
                unityEventBinder = null;
            }

            disposed = true;
        }


        /// <summary>
        /// Get all bindable Unity events from a particular component.
        /// </summary>
        private static IEnumerable<BindableEvent> GetBindableEvents(Component component)
        {
            Assert.IsNotNull(component, "Cannot get bindinable events of a null component.");

            var type = component.GetType();

            var bindableEventsFromProperties = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(propertyInfo => propertyInfo.PropertyType.IsSubclassOf(typeof(UnityEventBase)))
                .Where(propertyInfo => !propertyInfo.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any())
                .Select(propertyInfo => new BindableEvent()
                {
                    UnityEvent = (UnityEventBase)propertyInfo.GetValue(component, null),
                    Name = propertyInfo.Name,
                    DeclaringType = propertyInfo.DeclaringType,
                    ComponentType = component.GetType()
                });

            var bindableEventsFromFields = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(fieldInfo => fieldInfo.FieldType.IsSubclassOf(typeof(UnityEventBase)))
                .Where(fieldInfo => !fieldInfo.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any())
                .Select(fieldInfo => new BindableEvent
                {
                    UnityEvent = (UnityEventBase)fieldInfo.GetValue(component),
                    Name = fieldInfo.Name,
                    DeclaringType = fieldInfo.DeclaringType,
                    ComponentType = type
                });

            return bindableEventsFromFields.Concat(bindableEventsFromProperties);
        }

        /// <summary>
        /// Get a particular Unity event from a component.
        /// </summary>
        private static BindableEvent GetBoundEvent(string boundEventName, Component component)
        {
            Assert.IsNotNull(component);
            Assert.IsFalse(string.IsNullOrEmpty(boundEventName));

            var componentType = component.GetType();
            var boundEvent = GetBindableEvents(component)
                .FirstOrDefault(e => e.Name.Equals(boundEventName));

            if (boundEvent == null)
            {
                throw new InvalidEventException(string.Format("Could not bind to event \"{0}\" on component \"{1}\".", boundEventName, componentType));
            }

            return boundEvent;
        }

        /// <summary>
        /// Get all bindable Unity events on a particular game object.
        /// </summary>
        public static BindableEvent[] GetBindableEvents(GameObject gameObject) //todo: Consider moving this to TypeResolver.
        {
            Assert.IsNotNull(gameObject);

            var buffer = Buffer.Components;
            gameObject.GetComponents(buffer);

            return buffer
                .Where(component => component != null)
                .SelectMany(GetBindableEvents)
                .ToArray();
        }
    }
}
