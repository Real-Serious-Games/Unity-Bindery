using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding.Adapters
{
    public static class BaseAdapters
    {
        /// <summary>
        /// Return inverted bool value 
        /// </summary>
        public static bool BoolInversion(bool inValue)
        {
            return !inValue;
        }
        
        /// <summary>
        /// Returns either the value of TrueColor from the specified adapter options if
        /// the input value was true, or FalseColor if it was false.
        /// </summary>
        public static Color BoolToColor(bool valueIn, BoolToColorAdapterOptions options)
        {
            return valueIn ? options.TrueColor : options.FalseColor;
        }
        
        /// <summary>
        /// Returns either the value of TrueColors from the specified adapter options if
        /// the input value was true, or FalseColors if it was false.
        /// </summary>
        public static ColorBlock BoolToColorBlock(bool valueIn, BoolToColorBlockAdapterOptions options)
        {
            return valueIn ? options.TrueColors : options.FalseColors;
        }

        /// <summary>
        /// Adapter for converting from a bool to a string.
        /// </summary>
        public static string BoolToString(bool valueIn, BoolToStringAdapterOptions options)
        {
            return valueIn ? options.TrueValueString : options.FalseValueString;
        }
        
        /// <summary>
        /// Adapter that converts a single Color to one of the colors inside a ColorBlock
        /// </summary>
        public static ColorBlock ColorToColorBlock(Color valueIn, ColorToColorBlockAdapterOptions options)
        {
            var colorBlock = options.DefaultColors;
            switch (options.OverrideColor)
            {
                case ColorToColorBlockAdapterOptions.Role.Disabled:
                    colorBlock.disabledColor = valueIn;
                    break;
                case ColorToColorBlockAdapterOptions.Role.Highlighed:
                    colorBlock.highlightedColor = valueIn;
                    break;
                case ColorToColorBlockAdapterOptions.Role.Normal:
                    colorBlock.normalColor = valueIn;
                    break;
                case ColorToColorBlockAdapterOptions.Role.Pressed:
                    colorBlock.pressedColor = valueIn;
                    break;
            }

            return colorBlock;
        }
        
        /// <summary>
        /// Adapter for converting from a DateTime to an OADate as a float.
        /// </summary>
        public static float DateTimeToOADate(DateTime valueIn)
        {
            return (float)valueIn.ToOADate();
        }

        /// <summary>
        /// Adapter for converting from a DateTime to a string.
        /// </summary>
        public static string DateTimeToString(DateTime valueIn, DateTimeToStringAdapterOptions options)
        {
            return valueIn.ToString(options.Format);
        }
        
        /// <summary>
        /// Adapter for converting from a float as an OADate to a DateTime.
        /// </summary>
        public static DateTime FloatToDateTime(float valueIn)
        {
            return DateTime.FromOADate(valueIn);
        }

        /// <summary>
        /// Adapter that converts a float to a string.
        /// </summary>
        public static string FloatToString(float valueIn, FloatToStringAdapterOptions options)
        {
            return valueIn.ToString(options.Format);
        }

        /// <summary>
        /// Adapter for converting from an int to a string.
        /// </summary>
        public static string IntToString(int valueIn)
        {
            return valueIn.ToString();
        }

        /// <summary>
        /// Adapter for converting from a string to a DateTime, using a specified culture.
        /// </summary>
        public static DateTime StringCultureToDateTime(string valueIn, StringCultureToDateTimeAdapterOptions options)
        {
            var culture = new CultureInfo(options.CultureName);
            return DateTime.Parse(valueIn, culture);
        }

        /// <summary>
        /// String to bool adapter that returns false if the string is null or empty, 
        /// otherwise true.
        /// </summary>
        public static bool StringEmptyToBool(string valueIn)
        {
            return !string.IsNullOrEmpty(valueIn);
        }

        /// <summary>
        /// Adapter that parses a string as a float.
        /// </summary>
        public static float StringToFloat(string valueIn)
        {
            return float.Parse(valueIn);
        }

        /// <summary>
        /// Adapter for converting from a string to an int.
        /// </summary>
        public static int StringToInt(string valueIn)
        {
            return int.Parse(valueIn);
        }

        public static void RegisterBaseAdapters()
        {
            TypeResolver.RegisterAdapter(new AdapterInfo<bool, bool>(BoolInversion, nameof(BoolInversion)));
            TypeResolver.RegisterAdapter(new AdapterInfo<bool, Color, BoolToColorAdapterOptions>(BoolToColor, nameof(BoolToColor)));
            TypeResolver.RegisterAdapter(new AdapterInfo<bool, ColorBlock, BoolToColorBlockAdapterOptions>(BoolToColorBlock, nameof(BoolToColorBlock)));
            TypeResolver.RegisterAdapter(new AdapterInfo<bool, string, BoolToStringAdapterOptions>(BoolToString, nameof(BoolToString)));
            TypeResolver.RegisterAdapter(new AdapterInfo<Color, ColorBlock, ColorToColorBlockAdapterOptions>(ColorToColorBlock, nameof(ColorToColorBlock)));
            TypeResolver.RegisterAdapter(new AdapterInfo<DateTime, float>(DateTimeToOADate, nameof(DateTimeToOADate)));
            TypeResolver.RegisterAdapter(new AdapterInfo<DateTime, string, DateTimeToStringAdapterOptions>(DateTimeToString, nameof(DateTimeToString)));
            TypeResolver.RegisterAdapter(new AdapterInfo<float, DateTime>(FloatToDateTime, nameof(FloatToDateTime)));
            TypeResolver.RegisterAdapter(new AdapterInfo<float, string, FloatToStringAdapterOptions>(FloatToString, nameof(FloatToString)));
            TypeResolver.RegisterAdapter(new AdapterInfo<int, string>(IntToString, nameof(IntToString)));
            TypeResolver.RegisterAdapter(new AdapterInfo<string, DateTime, StringCultureToDateTimeAdapterOptions>(StringCultureToDateTime, nameof(StringCultureToDateTime)));
            TypeResolver.RegisterAdapter(new AdapterInfo<string, bool>(StringEmptyToBool, nameof(StringEmptyToBool)));
            TypeResolver.RegisterAdapter(new AdapterInfo<string, float>(StringToFloat, nameof(StringToFloat)));
            TypeResolver.RegisterAdapter(new AdapterInfo<string, int>(StringToInt, nameof(StringToInt)));
        }
    }
}