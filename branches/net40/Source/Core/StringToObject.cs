using System;
using System.ComponentModel;

namespace NConsoler
{
	public static class StringToObject
	{
		public static object ConvertValue(string value, Type argumentType)
		{
			if (value == String.Empty)
			{
				return GetDefault(argumentType);
			}

			if(IsNullableType(argumentType))
			{
				argumentType = Nullable.GetUnderlyingType(argumentType);
			}

			if (argumentType == typeof(String))
			{
				return value;
			}

			if (argumentType == typeof(DateTime))
			{
				return ConvertToDateTime(value);
			}

			if (argumentType == typeof(string[]))
			{
				return value.Split('+');
			}

			if (argumentType == typeof(int[]))
			{
				string[] values = value.Split('+');
				var valuesArray = new int[values.Length];
				for (int i = 0; i < values.Length; i++)
				{
					valuesArray[i] = (int)ConvertValue(values[i], typeof(int));
				}
				return valuesArray;
			}

			// The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32,
			// UInt32, Int64, UInt64, Char, Double, and Single
			if (argumentType.IsPrimitive || argumentType == typeof(decimal) || argumentType.IsEnum)
			{
				try
				{
					var converter = TypeDescriptor.GetConverter(argumentType);
					return converter.ConvertFromString(value);
				}
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to {1}", value, argumentType.ToString());
				}
				catch (OverflowException)
				{
					throw new NConsolerException("Value \"{0}\" is too big or too small", value);
				}
			}

			throw new NConsolerException("Unknown type is used in your method: {0}", argumentType.FullName);
		}

		private static DateTime ConvertToDateTime(string parameter)
		{
			string[] parts = parameter.Split('-');
			if (parts.Length != 3)
			{
				throw new NConsolerException("Could not convert {0} to Date", parameter);
			}
			var day = (int)ConvertValue(parts[0], typeof(int));
			var month = (int)ConvertValue(parts[1], typeof(int));
			var year = (int)ConvertValue(parts[2], typeof(int));
			try
			{
				return new DateTime(year, month, day);
			}
			catch (ArgumentException)
			{
				throw new NConsolerException("Could not convert {0} to Date", parameter);
			}
		}

		public static bool CanBeConvertedToDate(string parameter)
		{
			try
			{
				ConvertToDateTime(parameter);
				return true;
			}
			catch (NConsolerException)
			{
				return false;
			}
		}

		static bool IsNullableType(Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;
		}

		public static object GetDefault(Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}
	}
}
