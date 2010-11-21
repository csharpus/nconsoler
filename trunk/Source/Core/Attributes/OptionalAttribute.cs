namespace NConsoler
{
	/// <summary>
	/// Marks an Action method parameter as optional
	/// </summary>
	public sealed class OptionalAttribute : ParameterAttribute
	{
		public string[] AltNames { get; set; }
		public object Default { get; private set; }

		/// <param name="defaultValue">Default value if client doesn't pass this value</param>
		/// <param name="altNames">Aliases for parameter</param>
		public OptionalAttribute(object defaultValue, params string[] altNames)
		{
			Default = defaultValue;
			AltNames = altNames;
		}
	}
}