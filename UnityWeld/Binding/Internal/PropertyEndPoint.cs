using System;
using System.Reflection;
using UnityEngine;
using UnityWeld.Binding.Adapters;

namespace UnityWeld.Binding.Internal
{
    /// <summary>
    /// Represents an attachment to a property via reflection.
    /// </summary>
    public class PropertyEndPoint
    {
        /// <summary>
        /// The object that owns the property.
        /// </summary>
        private readonly object propertyOwner;

        /// <summary>
        /// The name of the property.
        /// </summary>
        private readonly string propertyName;

        /// <summary>
        /// Cached reference to the property.
        /// </summary>
        private readonly PropertyInfo property;

        /// <summary>
        /// Adapter for converting values that are set on the property.
        /// </summary>
        private readonly IAdapterInfo adapter;

        /// <summary>
        /// Options for using the adapter to convert values.
        /// </summary>
        private readonly AdapterOptions adapterOptions;

        /// <summary>
        /// Create a new property end point.
        /// </summary>
        /// <param name="propertyOwner">The object owning the bound property.</param>
        /// <param name="propertyName">The string name of the property.</param>
        /// <param name="adapter">Adapter (can be null for no adapter).</param>
        /// <param name="options">Adapter options (can be null for adapters that do not
        /// require options or if there is no adapter).</param>
        /// <param name="endPointType">The string name of the type of the object
        /// containing the bound property.</param>
        /// <param name="context">Unity component that the property is connected to, used
        /// so that clicking an error message in the log will highlight the relevant
        /// component in the scene.</param>
        public PropertyEndPoint(
            object propertyOwner, 
            string propertyName, 
            IAdapterInfo adapter, 
            AdapterOptions options, 
            string endPointType, 
            Component context)
        {
            this.propertyOwner = propertyOwner;
            this.adapter = adapter;
            this.adapterOptions = options;
            var type = propertyOwner.GetType();

            if (string.IsNullOrEmpty(propertyName))
            {
                Debug.LogError(
                    "Property not specified for type '" + type + "'.", context
                );
                return;
            }

            this.propertyName = propertyName;
            this.property = type.GetProperty(propertyName);

            if (this.property == null)
            {
                Debug.LogError(
                    "Property '" + propertyName + "' not found on " + endPointType  
                    + " '" + type + "'.", context
                );
            }
        }

        /// <summary>
        /// Get the value of the property.
        /// </summary>
        public object GetValue()
        {
            return property != null ? property.GetValue(propertyOwner, null) : null;
        }

        /// <summary>
        /// Set the value of the property.
        /// </summary>
        public void SetValue(object input)
        {
            if (property == null)
            {
                return;
            }

            if (adapter != null)
            {
                input = adapter.Convert(input, adapterOptions);
            }

            property.SetValue(propertyOwner, input, null);
        }

        /// <summary>
        /// Get a string representation of this property end point.
        /// </summary>
        /// <returns>String in the following format: "PropertyOwner.PropertyName
        /// (PropertyType)", or an error message if the property was not found.</returns>
        public override string ToString()
        {
            if (property == null)
            {
                return "!! property not found !!";
            }

            return string.Concat(
                propertyOwner.GetType(), 
                ".", 
                property.Name, 
                " (", 
                property.PropertyType.Name, 
                ")"
            );
        }

        /// <summary>
        /// Watch the property for changes.
        /// </summary>
        public PropertyWatcher Watch(Action changed)
        {
            return new PropertyWatcher(propertyOwner, propertyName, changed);
        }
    }
}
