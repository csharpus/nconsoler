﻿namespace NConsoler
{
    using System;

    /// <summary>
    /// Should not be used directly
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ParameterAttribute : Attribute
    {
        /// <summary>
        /// Description is used in help message
        /// </summary>
        public string Description { get; set; }

        protected ParameterAttribute() => this.Description = string.Empty;
    }
}