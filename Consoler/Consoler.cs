using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Consoler
{
	public class Consoler
	{
		public static void Run(Type targetType, string[] args)
		{
			MethodInfo[] methods = typeof(Program).GetMethods(BindingFlags.Public | BindingFlags.Static);
			List<MethodInfo> actionMethods = new List<MethodInfo>();
			foreach (MethodInfo method in methods)
			{
				object[] attributes = method.GetCustomAttributes(false);
				foreach (object attribute in attributes)
				{
					if (attribute is ActionAttribute)
					{
						actionMethods.Add(method);
						break;
					}
				}
			}
			List<object> parameterValues = new List<object>();
			int argumentIndex = 0;
			foreach (ParameterInfo info in methods[0].GetParameters())
			{
				if (info.ParameterType == typeof(int))
				{
					parameterValues.Add(Convert.ToInt32(args[argumentIndex]));
					continue;
				}
				if (info.ParameterType == typeof(string))
				{
					parameterValues.Add(Convert.ToString(args[argumentIndex]));
					continue;
				}
				if (info.ParameterType == typeof(bool))
				{
					parameterValues.Add(true);//Convert.ToBoolean(args[argumentIndex]));
					continue;
				}
				argumentIndex++;
			}
			methods[0].Invoke(null, parameterValues.ToArray());
		}
	}

	public sealed class ActionAttribute : Attribute
	{
	}

	public class ParameterAttribute : Attribute
	{
		private string _description;

		public string Description
		{
			get
			{
				return _description;
			}

			set
			{
				_description = value;
			}
		}

		private string[] _altNames;

		public string[] AltNames
		{
			get
			{
				return _altNames;
			}

			set
			{
				_altNames = value;
			}
		}

		private bool _isRequired;

		protected ParameterAttribute(bool isRequired, params string[] altNames)
		{
			_isRequired = isRequired;
			_altNames = altNames;
		}
	}

	public sealed class OptionalAttribute : ParameterAttribute
	{
		public OptionalAttribute(params string[] altNames) 
			: base(false, altNames)
		{
		}
	}

	public sealed class RequiredAttribute : ParameterAttribute
	{
		public RequiredAttribute(params string[] altNames)
			: base(true, altNames)
		{
		}
	}
}
