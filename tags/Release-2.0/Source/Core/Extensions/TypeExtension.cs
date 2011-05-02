namespace NConsoler.Extensions
{
	using System;

	public static class TypeExtension
	{
		public static bool CanBeNull(this Type type)
		{
			return type == typeof(string)
				   || type == typeof(string[])
				   || type == typeof(int[]);
		}
	}
}
