using System;

namespace NConsoler
{
	/// <summary>
	/// Every action method should be marked with this attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class ActionAttribute : Attribute
	{
		public ActionAttribute()
		{
		}

		public ActionAttribute(string description)
		{
			_description = description;
		}

		private string _description = String.Empty;

		/// <summary>
		/// Description is used for help messages
		/// </summary>
		public string Description
		{
			get { return _description; }

			set { _description = value; }
		}
	}
}