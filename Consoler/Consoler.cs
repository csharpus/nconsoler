using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Consoler
{
	public sealed class Consoler
	{
		public static void Run(Type targetType, string[] args)
		{
			new Consoler(targetType, args).Run();
		}

		private Type _targetType;
		private string[] _args;
		private List<MethodInfo> _actionMethods = new List<MethodInfo>();

		private Consoler(Type targetType, string[] args)
		{
			_targetType = targetType;
			_args = args;
			MethodInfo[] methods = typeof(Program).GetMethods(BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo method in methods)
			{
				object[] attributes = method.GetCustomAttributes(false);
				foreach (object attribute in attributes)
				{
					if (attribute is ActionAttribute)
					{
						_actionMethods.Add(method);
						break;
					}
				}
			}
		}

		private void Run()
		{
			Validate();
			
			//List<object> parameterValues = new List<object>();
			//int argumentIndex = 0;
			//foreach (ParameterInfo info in methods[0].GetParameters())
			//{
			//    if (info.ParameterType == typeof(int))
			//    {
			//        parameterValues.Add(Convert.ToInt32(_args[argumentIndex]));
			//        continue;
			//    }
			//    if (info.ParameterType == typeof(string))
			//    {
			//        parameterValues.Add(Convert.ToString(_args[argumentIndex]));
			//        continue;
			//    }
			//    if (info.ParameterType == typeof(bool))
			//    {
			//        parameterValues.Add(true);//Convert.ToBoolean(args[argumentIndex]));
			//        continue;
			//    }
			//    argumentIndex++;
			//}
			//methods[0].Invoke(null, parameterValues.ToArray());
		}

		private bool Validate()
		{
			if (_actionMethods.Count == 0)
			{
				Console.WriteLine("Can not find any public static method in type \"{0}\" in assembly \"{1}\" marked with [Action] attribute ", _targetType.FullName, _targetType.Assembly.FullName);
				return false;
			}
			foreach (MethodInfo methodInfo in _actionMethods)
			{
				bool optionalFound = false;
				foreach (ParameterInfo info in methodInfo.GetParameters())
				{
					object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
					if (attributes.Length > 1)
					{
						Console.WriteLine("More than one attribute is applied to parameter {0} in method {1} in type {2}", info.Name, methodInfo.Name, _targetType.FullName);
						return false;
					}
					if (attributes.Length == 1 && attributes[0].GetType() == typeof(OptionalAttribute))
					{
						optionalFound = true;
						OptionalAttribute optional = (attributes[0] as OptionalAttribute);
						if (!optional.Default.GetType().IsAssignableFrom(info.ParameterType))
						{
							Console.WriteLine("Default value for an optional parameter \"{0}\" in method \"{1}\" in type \"{2}\" can not be assigned to the parameter", info.Name, methodInfo.Name, _targetType.FullName);
							return false;
						}
					}
					if (optionalFound && (attributes.Length == 0 || attributes[0].GetType() == typeof(RequiredAttribute)))
					{
						Console.WriteLine("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"", methodInfo.Name, info.Name);
						return false;
					}
				}
			}
			return true;
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

		private bool _isRequired;

		protected ParameterAttribute(bool isRequired)
		{
			_isRequired = isRequired;
		}
	}

	public sealed class OptionalAttribute : ParameterAttribute
	{
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

		private object _defaultValue;

		public object Default
		{
			get
			{
				return _defaultValue;
			}
		}

		public OptionalAttribute(object defaultValue, params string[] altNames) 
			: base(false)
		{
			_defaultValue = defaultValue;
			_altNames = altNames;
		}
	}

	public sealed class RequiredAttribute : ParameterAttribute
	{
		public RequiredAttribute()
			: base(true)
		{
			
		}
	}
}
