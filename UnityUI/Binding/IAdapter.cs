﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityUI.Binding
{
    /// <summary>
    /// Base interface for all adapters. Combine with AdapterAttribute to specify 
    /// the types it supports converting to and from.
    /// </summary>
    interface IAdapter
    {
        /// <summary>
        /// Convert from the source type to the output type. This should throw an exception 
        /// if the conversion fails or the input isn't valid.
        /// </summary>
        object GetValue(object valueIn);
    }
}