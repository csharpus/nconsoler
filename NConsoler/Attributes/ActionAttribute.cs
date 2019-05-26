namespace NConsoler
{
    using System;

    /// <summary>
    /// Every action method should be marked with this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ActionAttribute : Attribute
    {
        public ActionAttribute() => this.Description = string.Empty;

        public ActionAttribute(string description) => this.Description = description;

        /// <summary>
        /// Description is used for help messages
        /// </summary>
        public string Description { get; set; }
    }
}