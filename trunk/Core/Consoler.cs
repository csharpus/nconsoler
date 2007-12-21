using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NConsoler
{
	public sealed class Consoler
	{
		public static void Run(Type targetType, string[] args)
		{
			Run(targetType, args, new ConsoleMessenger());
		}

		public static void Run(Type targetType, string[] args, IMessenger messenger)
		{
			try
			{
				new Consoler(targetType, args).Run();
			}
			catch (NConsoleException e)
			{
				messenger.Write(e.Message);
			}
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

		private bool IsRequired(ParameterInfo info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
			return attributes.Length == 0 || attributes[0].GetType() == typeof(RequiredAttribute);
		}

		object ConvertValue(string value, Type argumentType)
		{
				if (argumentType == typeof(int))
				{
					try
					{
						return Convert.ToInt32(value);
					}
					catch (FormatException fe)
					{
						throw new NConsoleException("Could not convert \"{0}\" to integer", value);
					}
					catch (OverflowException oe)
					{
						throw new NConsoleException("Value \"{0}\" is too big or too small", value);
					}
				}
				if (argumentType == typeof(string))
				{
					return value;
				}
				if (argumentType == typeof(bool))
				{
					try
					{
						return Convert.ToBoolean(value);
					}
					catch (FormatException fe)
					{
						throw new NConsoleException("Could not convert \"{0}\" to boolean", value);
					}
				}
			throw new NConsoleException("Unknown type is used in your method {0}", argumentType.FullName);
		}

		private struct ParameterData
		{
			public string primaryName;
			public int position;
			public Type type;

			public ParameterData(int position, Type type, string primaryName)
			{
				this.position = position;
				this.type = type;
				this.primaryName = primaryName;
			}
		}

		private void Run()
		{
			Validate();
			if (_actionMethods.Count == 1)
			{
				int argumentIndex = 0;
				List<object> parameterValues = new List<object>();
				Dictionary<string, ParameterData> aliases = new Dictionary<string, ParameterData>();
				int requiredParameterCount = 0;
				List<string> passedOptionalAliases = new List<string>();
				foreach (ParameterInfo info in _actionMethods[0].GetParameters())
				{
					if (IsRequired(info))
					{
						requiredParameterCount++;
						if (_args.Length < requiredParameterCount)
						{
							throw new NConsoleException("Not all required parameters is set");
						}
						parameterValues.Add(ConvertValue(_args[argumentIndex], info.ParameterType));
						
					}
					else // /a /a:value /-a
					{
						
						object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
						OptionalAttribute optional = attributes[0] as OptionalAttribute;
						
						foreach (string altName in optional.AltNames)
						{
							aliases.Add(altName, 
								new ParameterData(parameterValues.Count, info.ParameterType, info.Name));
						}
						aliases.Add(info.Name, 
							new ParameterData(parameterValues.Count, info.ParameterType, info.Name));
						parameterValues.Add(optional.Default);
					}
					argumentIndex++;
				}
				Dictionary<string, string> values = new Dictionary<string, string>();
				for (int i = requiredParameterCount; i < _args.Length; i++)
				{
					if (!_args[i].StartsWith("/"))
					{
						throw new NConsoleException("Unknown parameter {0}", _args[i]);
					}
					if (_args[i].Contains(":"))
					{
						int semicolonPosition = _args[i].IndexOf(':');
						string name = _args[i].Substring(1, semicolonPosition - 1);
						string value = _args[i].Substring(semicolonPosition + 1);
						if (!aliases.ContainsKey(name))
						{
							throw new NConsoleException("Unknown parameter name {0}", _args[i]);
						}
						if (passedOptionalAliases.Contains(aliases[name].primaryName))
						{
#warning use real names, and if two parameters with different aliases but the same primary names are passed show specified error
							throw new NConsoleException("Parameter with name {0} passed two times", aliases[name].primaryName);
						}
						passedOptionalAliases.Add(aliases[name].primaryName);
						parameterValues[aliases[name].position] = ConvertValue(value, aliases[name].type);

					}
					else if (_args[i].Contains("-"))
					{
						string name = _args[i].Substring(2);
						if (!aliases.ContainsKey(name))
						{
							throw new NConsoleException("Unknown parameter name {0}", _args[i]);
						}
						parameterValues[aliases[name].position] = ConvertValue("false", aliases[name].type);
					}
					else
					{
						string name = _args[i].Substring(1);
						if (!aliases.ContainsKey(name))
						{
							throw new NConsoleException("Unknown parameter name {0}", _args[i]);
						}
						parameterValues[aliases[name].position] = ConvertValue("true", aliases[name].type);
					}
				}
				_actionMethods[0].Invoke(null, parameterValues.ToArray());
			}
		}

		private void Validate()
		{
			if (_actionMethods.Count == 0)
			{
				throw new NConsoleException("Can not find any public static method in type \"{0}\" in assembly \"{1}\" marked with [Action] attribute ", _targetType.FullName, _targetType.Assembly.FullName);
			}
			int requiredParameterCount = 0;
			foreach (MethodInfo methodInfo in _actionMethods)
			{
				List<string> parameterNames = new List<string>();
				bool optionalFound = false;
				foreach (ParameterInfo info in methodInfo.GetParameters())
				{
					object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
					if (attributes.Length > 1)
					{
						throw new NConsoleException("More than one attribute is applied to parameter {0} in method {1} in type {2}", info.Name, methodInfo.Name, _targetType.FullName);
					}
					if (attributes.Length == 1 && attributes[0].GetType() == typeof(OptionalAttribute))
					{
						optionalFound = true;
						OptionalAttribute optional = (attributes[0] as OptionalAttribute);
						if (!optional.Default.GetType().IsAssignableFrom(info.ParameterType))
						{
							throw new NConsoleException("Default value for an optional parameter \"{0}\" in method \"{1}\" in type \"{2}\" can not be assigned to the parameter", info.Name, methodInfo.Name, _targetType.FullName);
						}
					}
					if (IsRequired(info))
					{
						requiredParameterCount++;
						parameterNames.Add(info.Name);
					}
					else
					{
						if (parameterNames.Contains(info.Name))
						{
							throw new NConsoleException("Found duplicated parameter name {0} in method {1}. Please check alt names for optional parameters", info.Name, methodInfo.Name);
						}
						parameterNames.Add(info.Name);
						OptionalAttribute optional = attributes[0] as OptionalAttribute;
						foreach (string altName in optional.AltNames)
						{
							if (parameterNames.Contains(altName))
							{
								throw new NConsoleException("Found duplicated parameter name {0} in method {1}. Please check alt names for optional parameters", altName, methodInfo.Name);
							}
						}
					}
					if (optionalFound && IsRequired(info))
					{
						throw new NConsoleException("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"", methodInfo.Name, info.Name);
					}
				}
			}
		}
	}

	public interface IMessenger
	{
		void Write(string message);
	}

	public class ConsoleMessenger : IMessenger
	{
		public void Write(string message)
		{
			Console.WriteLine(message);
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

	sealed class NConsoleException : Exception
	{
		public NConsoleException(string message, params string[] arguments)
			: base(String.Format(message, arguments))
		{
		}
	}
}
