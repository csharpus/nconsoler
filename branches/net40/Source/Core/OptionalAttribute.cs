namespace NConsoler
{
	/// <summary>
	/// Marks an Action method parameter as optional
	/// </summary>
	public sealed class OptionalAttribute : ParameterAttribute
	{
		private string[] _altNames;

		public string[] AltNames
		{
			get { return _altNames; }

			set { _altNames = value; }
		}

		private readonly object _defaultValue;

		public object Default
		{
			get { return _defaultValue; }
		}

		/// <param name="defaultValue">Default value if client doesn't pass this value</param>
		/// <param name="altNames">Aliases for parameter</param>
		public OptionalAttribute(object defaultValue, params string[] altNames)
		{
			_defaultValue = defaultValue;
			_altNames = altNames;
		}
	}
}