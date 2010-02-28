using System;

namespace NConsoler
{
	/// <summary>
	/// Should not be used directly
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class ParameterAttribute : Attribute
	{
		private string _description = String.Empty;

		/// <summary>
		/// Description is used in help message
		/// </summary>
		public string Description
		{
			get { return _description; }

			set { _description = value; }
		}

		protected ParameterAttribute()
		{
		}
	}
}