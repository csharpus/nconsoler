using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NConsoler
{
	public sealed class Consolery
	{
		public static void Run(Type targetType, string[] args)
		{
			Run(targetType, args, new ConsoleMessenger());
		}

		public static void Run(Type targetType, string[] args, IMessenger messenger)
		{
			try
			{
				new Consolery(targetType, args, messenger).Run();
			}
			catch (NConsolerException e)
			{
				messenger.Write(e.Message);
			}
		}

		private Type _targetType;
		private string[] _args;
		private List<MethodInfo> _actionMethods = new List<MethodInfo>();
		private IMessenger _messenger;

		private Consolery(Type targetType, string[] args, IMessenger messenger)
		{
			_targetType = targetType;
			_args = args;
			_messenger = messenger;
			MethodInfo[] methods = _targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
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

		private bool IsOptional(ParameterInfo info)
		{
			return !IsRequired(info);
		}

		private OptionalAttribute GetOptional(ParameterInfo info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(OptionalAttribute), false);
			return attributes[0] as OptionalAttribute;
		}

		private RequiredAttribute GetRequired(ParameterInfo info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(RequiredAttribute), false);
			return attributes[0] as RequiredAttribute;
		}

		object ConvertValue(string value, Type argumentType)
		{
			if (argumentType == typeof(int))
			{
				try
				{
					return Convert.ToInt32(value);
				}
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to integer", value);
				}
				catch (OverflowException)
				{
					throw new NConsolerException("Value \"{0}\" is too big or too small", value);
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
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to boolean", value);
				}
			}
			if (argumentType == typeof(string[]))
			{
				return value.Split('+');
			}
			if (argumentType == typeof(int[]))
			{
				string[] values = value.Split('+');
				int[] valuesArray = new int[values.Length];
				for (int i = 0; i < values.Length; i++)
				{
					valuesArray[i] = (int)ConvertValue(values[i], typeof(int));
				}
				return valuesArray;
			}
			if (argumentType == typeof(DateTime))
			{
				string[] parts = value.Split('-');
				if (parts.Length != 3)
				{
					throw new NConsolerException("Could not convert {0} to Date", value);
				}
				int day = (int)ConvertValue(parts[0], typeof(int));
				int month = (int)ConvertValue(parts[1], typeof(int));
				int year = (int)ConvertValue(parts[2], typeof(int));
				try
				{
					return new DateTime(year, month, day);
				}
				catch(ArgumentException)
				{
					throw new NConsolerException("Could not convert {0} to Date", value);
				}
			}
			throw new NConsolerException("Unknown type is used in your method {0}", argumentType.FullName);
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
			ValidateMetadata();
			if (_args.Length == 0 || _args[0] == "/?" || _args[0] == "/help")
			{
				PrintUsage();
				return;
			}
			MethodInfo currentMethod = GetCurrentMethod();
			ValidateInput(currentMethod);
			currentMethod.Invoke(null, BuildParameterArray(currentMethod));
		}

		private object[] BuildParameterArray(MethodInfo method)
		{
#warning check option parameter
			int argumentIndex = 0;
			List<object> parameterValues = new List<object>();
			Dictionary<string, ParameterData> aliases = new Dictionary<string, ParameterData>();
			foreach (ParameterInfo info in method.GetParameters())
			{
				if (IsRequired(info))
				{
					parameterValues.Add(ConvertValue(_args[argumentIndex], info.ParameterType));
				}
				else
				{
					OptionalAttribute optional = GetOptional(info);

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
			for (int i = RequiredParameterCount(method); i < _args.Length; i++)
			{
				string name = ParameterName(_args[i]);
				string value = ParameterValue(_args[i]);
				parameterValues[aliases[name].position] = ConvertValue(value, aliases[name].type);
			}
			return parameterValues.ToArray();
		}

		private int RequiredParameterCount(MethodInfo method)
		{
			int requiredParameterCount = 0;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					requiredParameterCount++;
				}
			}
			return requiredParameterCount;
		}

		private MethodInfo GetCurrentMethod()
		{
			if (_actionMethods.Count == 1)
			{
				return _actionMethods[0];
			}
			else
			{
				if (_args.Length == 0)
				{
					PrintUsage();
					throw new NConsolerException("Error");
				}
				string methodName = _args[0].ToLower();
				foreach (MethodInfo method in _actionMethods)
				{
					if (method.Name.ToLower() == methodName)
					{
						return method;
					}
				}
				PrintUsage();
				throw new NConsolerException("Unknown option {0}, print usage", _args[0]);
			}
		}

		private void PrintUsage()
		{
			foreach (MethodInfo method in _actionMethods)
			{
				object[] actionAttributes = method.GetCustomAttributes(typeof(ActionAttribute), false);
				ActionAttribute action = actionAttributes[0] as ActionAttribute;
				if (_actionMethods.Count > 1)
				{
					_messenger.Write(method.Name.ToLower() + " " + action.Description);
					_messenger.Write("");
				}
				// _messenger.Write("Usage: program ");
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				foreach (ParameterInfo parameter in method.GetParameters())
				{
					object[] parameterAttributes = 
						parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
					if (parameterAttributes.Length > 0)
					{
						string name = GetDisplayName(parameter);
						ParameterAttribute attribute = parameterAttributes[0] as ParameterAttribute;
						parameters.Add(name, attribute.Description);
					}
					else
					{
						parameters.Add(parameter.Name, String.Empty);
					}
				}
				AssemblyName aName = new AssemblyName(Assembly.GetEntryAssembly().FullName);
				_messenger.Write("Usage: " + aName.Name + ".exe " + String.Join(" ", new List<string>(parameters.Keys).ToArray()));
				int maxLength = 0;
				foreach (KeyValuePair<string, string> pair in parameters)
				{
					if (pair.Key.Length > maxLength)
					{
						maxLength = pair.Key.Length;
					}
				}
				foreach (KeyValuePair<string, string> pair in parameters)
				{
					if (pair.Value != String.Empty)
					{
						int difference = maxLength - pair.Key.Length + 2;
						_messenger.Write("    " + pair.Key + new String(' ', difference) + pair.Value);
					}
				}
			}
		}

		private string GetDisplayName(ParameterInfo parameter)
		{
			if (IsRequired(parameter))
			{
				return parameter.Name;
			}
			else
			{
				OptionalAttribute optional = GetOptional(parameter);
				string parameterName = 
					(optional.AltNames.Length > 0) ? optional.AltNames[0] : parameter.Name;
				if (parameter.ParameterType != typeof(bool))
				{
					parameterName += ":" + parameter.Name;
				}
				return "[/" + parameterName + "]";
			}
		}

		#region Validation

		private void ValidateInput(MethodInfo method)
		{
			CheckAllRequiredParametersAreSet(method);
			CheckOptionalParametersAreNotDuplicated(method);
			CheckUnknownParametersAreNotPassed(method);
		}

		private void CheckAllRequiredParametersAreSet(MethodInfo method)
		{
			if (_args.Length < RequiredParameterCount(method))
			{
				throw new NConsolerException("Not all required parameters are set");
			}
		}

		private string ParameterName(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return parameter.Substring(2);
			}
			else if (parameter.Contains(":"))
			{
				return parameter.Substring(1, parameter.IndexOf(":") - 1);
			}
			else
			{
				return parameter.Substring(1);
			}
		}

		private string ParameterValue(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return "false";
			}
			else if (parameter.Contains(":"))
			{
				return parameter.Substring(parameter.IndexOf(":") + 1);
			}
			else
			{
				return "true";
			}
		}

		private void CheckOptionalParametersAreNotDuplicated(MethodInfo method)
		{
			List<string> passedParameters = new List<string>();
			for (int i = RequiredParameterCount(method); i < _args.Length; i++)
			{
				if (!_args[i].StartsWith("/"))
				{
					throw new NConsolerException("Unknown parameter {0}", _args[i]);
				}
				string name = ParameterName(_args[i]);
				if (passedParameters.Contains(name))
				{
					throw new NConsolerException("Parameter with name {0} passed two times", name);
				}
				passedParameters.Add(name);
			}
		}

		private void CheckUnknownParametersAreNotPassed(MethodInfo method)
		{
			List<string> parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				parameterNames.Add(parameter.Name);
				OptionalAttribute optional = GetOptional(parameter);
				foreach (string altName in optional.AltNames)
				{
					parameterNames.Add(altName);
				}
			}
			for (int i = RequiredParameterCount(method); i < _args.Length; i++)
			{
				string name = ParameterName(_args[i]);
				if (!parameterNames.Contains(name))
				{
					throw new NConsolerException("Unknown parameter name {0}", _args[i]);
				}
			}
		}

		private void ValidateMetadata()
		{
			CheckAnyActionMethodExists();
			foreach (MethodInfo method in _actionMethods)
			{
				CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(method);
				CheckOptionalParametersAreAfterRequiredOnes(method);
				CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(method);
				CheckOptionalParametersAltNamesAreNotDuplicated(method);
			}
		}

		private void CheckAnyActionMethodExists()
		{
			if (_actionMethods.Count == 0)
			{
				throw new NConsolerException("Can not find any public static method in type \"{0}\" marked with [Action] attribute", _targetType.Name);
			}
		}

		private void CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(MethodInfo method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] attributes = parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
				if (attributes.Length > 1)
				{
					throw new NConsolerException("More than one attribute is applied to parameter \"{0}\" in method \"{1}\"", parameter.Name, method.Name);
				}
			}
		}

		private void CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(MethodInfo method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				OptionalAttribute optional = GetOptional(parameter);
				if (!optional.Default.GetType().IsAssignableFrom(parameter.ParameterType))
				{
					throw new NConsolerException("Default value for an optional parameter \"{0}\" in method \"{1}\" can not be assigned to the parameter", parameter.Name, method.Name);
				}
			}
		}

		private void CheckOptionalParametersAreAfterRequiredOnes(MethodInfo method)
		{
			bool optionalFound = false;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsOptional(parameter))
				{
					optionalFound = true;
				}
				else if (optionalFound)
				{
					throw new NConsolerException("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"", method.Name, parameter.Name);
				}
			}
		}

		private void CheckOptionalParametersAltNamesAreNotDuplicated(MethodInfo method)
		{
			List<string> parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					parameterNames.Add(parameter.Name);
				}
				else
				{
					if (parameterNames.Contains(parameter.Name))
					{
						throw new NConsolerException("Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters", parameter.Name, method.Name);
					}
					parameterNames.Add(parameter.Name);
					OptionalAttribute optional = GetOptional(parameter);
					foreach (string altName in optional.AltNames)
					{
						if (parameterNames.Contains(altName))
						{
							throw new NConsolerException("Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters", altName, method.Name);
						}
						parameterNames.Add(altName);
					}
				}
			}
		}

		#endregion
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
	}

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class ParameterAttribute : Attribute
	{
		private string _description = String.Empty;

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

	public sealed class NConsolerException : Exception
	{
		public NConsolerException(string message, params string[] arguments)
			: base(String.Format(message, arguments))
		{
		}
	}
}
