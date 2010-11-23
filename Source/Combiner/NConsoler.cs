namespace NConsoler
{
	using System;

	/// <summary>
	/// Every action method should be marked with this attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class ActionAttribute : Attribute
	{
		public ActionAttribute()
		{
			Description = String.Empty;
		}

		public ActionAttribute(string description)
		{
			Description = description;
		}

		/// <summary>
		/// Description is used for help messages
		/// </summary>
		public string Description { get; set; }
	}
}
namespace NConsoler
{
	using System;

	/// <summary>
	/// Uses Console class for message output
	/// </summary>
	public class ConsoleMessenger : IMessenger
	{
		public void Write(string message)
		{
			Console.WriteLine(message);
		}
	}
}
//
// NConsoler 1.1
// http://nconsoler.csharpus.com
//

namespace NConsoler
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Linq;
	using System.Reflection;
	using System.Diagnostics;

	/// <summary>
	/// Entry point for NConsoler applications
	/// </summary>
	public sealed class Consolery
	{
		/// <summary>
		/// Runs an appropriate Action method.
		/// Uses the class this call lives in as target type and command line arguments from Environment
		/// </summary>
		public static void Run()
		{
			Type declaringType = new StackTrace().GetFrame(1).GetMethod().DeclaringType;
			var args = new string[Environment.GetCommandLineArgs().Length - 1];
			new List<string>(Environment.GetCommandLineArgs()).CopyTo(1, args, 0, Environment.GetCommandLineArgs().Length - 1);
			Run(declaringType, args);
		}

		/// <summary>
		/// Runs an appropriate Action method
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		/// <param name="args">Arguments that will be converted to Action method arguments</param>
		public static void Run(Type targetType, string[] args)
		{
			Run(targetType, args, new ConsoleMessenger());
		}

		/// <summary>
		/// Runs an appropriate Action method
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		/// <param name="args">Arguments that will be converted to Action method arguments</param>
		/// <param name="messenger">Uses for writing messages instead of Console class methods</param>
		/// <param name="notationType">Switch for command line syntax. Windows: /param:value Linux: -param value</param>
		public static void Run(Type targetType, string[] args, IMessenger messenger, Notation notationType = Notation.Windows)
		{
			try
			{
				new Consolery(targetType, args, messenger, notationType).RunAction();
			}
			catch (NConsolerException e)
			{
				messenger.Write(e.Message);
			}
		}

		/// <summary>
		/// Validates specified type and throws NConsolerException if an error
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		public static void Validate(Type targetType)
		{
			new Consolery(targetType, new string[] {}, new ConsoleMessenger(), Notation.None).ValidateMetadata();
		}

		private readonly Type _targetType;
		private readonly string[] _args;
		private readonly List<MethodInfo> _actionMethods = new List<MethodInfo>();
		private readonly IMessenger _messenger;
		private readonly INotationStrategy _notation;
		private readonly Metadata _metadata;
		private readonly MetadataValidator _metadataValidator;

		public Consolery(Type targetType, string[] args, IMessenger messenger, Notation notationType)
		{
			Contract.Requires(targetType != null);
			Contract.Requires(args != null);
			Contract.Requires(messenger != null);

			_targetType = targetType;
			_args = args;
			_messenger = messenger;

			_actionMethods = _targetType
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(method => method.GetCustomAttributes(false).OfType<ActionAttribute>().Any())
				.ToList();

			_metadata = new Metadata(_actionMethods);
			_metadataValidator = new MetadataValidator(_targetType, _actionMethods, _metadata);
			if (notationType == Notation.Windows)
			{
				_notation = new WindowsNotationStrategy(_args, _messenger, _metadata);
			}
			else
			{
				_notation = new LinuxNotationStrategy(_args, _messenger, _metadata);
			}
		}

		private void RunAction()
		{
			ValidateMetadata();
			if (IsHelpRequested())
			{
				PrintUsage();
				return;
			}

			MethodInfo currentMethod = _notation.GetCurrentMethod();
			if (currentMethod == null)
			{
				PrintUsage();
				throw new NConsolerException("Unknown subcommand \"{0}\"", _args[0]);
			}
			_notation.ValidateInput(currentMethod);
			InvokeMethod(currentMethod);
		}

		private void ValidateMetadata()
		{
			_metadataValidator.ValidateMetadata();
		}

		public struct ParameterData
		{
			public readonly int Position;
			public readonly Type Type;

			public ParameterData(int position, Type type)
			{
				Position = position;
				Type = type;
			}
		}

		public class OptionalData
		{
			public OptionalData()
			{
				AltNames = new string[] {};
			}

			public string[] AltNames { get; set; }

			public object Default { get; set; }
		}

		private bool IsHelpRequested()
		{
			return (_args.Length == 0 && !_metadata.SingleActionWithOnlyOptionalParametersSpecified())
			       || (_args.Length > 0 && (_args[0] == "/?"
			                                || _args[0] == "/help"
			                                || _args[0] == "/h"
			                                || _args[0] == "help"));
		}

		private void InvokeMethod(MethodInfo method)
		{
			try
			{
				method.Invoke(null, _notation.BuildParameterArray(method));
			}
			catch (TargetInvocationException e)
			{
				if (e.InnerException != null)
				{
					throw new NConsolerException(e.InnerException.Message, e);
				}
				throw;
			}
		}

		#region Usage

		private void PrintUsage(MethodInfo method)
		{
			PrintMethodDescription(method);
			Dictionary<string, string> parameters = GetParametersDescriptions(method);
			PrintUsageExample(method, parameters);
			PrintParametersDescriptions(parameters);
		}

		private void PrintUsageExample(MethodInfo method, IDictionary<string, string> parameterList)
		{
			string subcommand = _metadata.IsMulticommand ? method.Name.ToLower() + " " : String.Empty;
			string parameters = String.Join(" ", new List<string>(parameterList.Keys).ToArray());
			_messenger.Write("usage: " + ProgramName() + " " + subcommand + parameters);
		}

		private void PrintMethodDescription(MethodInfo method)
		{
			string description = GetMethodDescription(method);
			if (description == String.Empty) return;
			_messenger.Write(description);
		}

		public string GetMethodDescription(MethodInfo method)
		{
			object[] attributes = method.GetCustomAttributes(true);
			foreach (ActionAttribute attribute in attributes.OfType<ActionAttribute>())
			{
				return attribute.Description;
			}
			throw new NConsolerException("Method is not marked with an Action attribute");
		}

		private Dictionary<string, string> GetParametersDescriptions(MethodInfo method)
		{
			var parameters = new Dictionary<string, string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] parameterAttributes =
					parameter.GetCustomAttributes(typeof (ParameterAttribute), false);
				if (parameterAttributes.Length > 0)
				{
					string name = GetDisplayName(parameter);
					var attribute = (ParameterAttribute) parameterAttributes[0];
					parameters.Add(name, attribute.Description);
				}
				else
				{
					parameters.Add(parameter.Name, String.Empty);
				}
			}
			return parameters;
		}

		private void PrintParametersDescriptions(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			int maxParameterNameLength = MaxKeyLength(parameters);
			foreach (KeyValuePair<string, string> pair in parameters)
			{
				if (pair.Value != String.Empty)
				{
					int difference = maxParameterNameLength - pair.Key.Length + 2;
					_messenger.Write("    " + pair.Key + new String(' ', difference) + pair.Value);
				}
			}
		}

		private static int MaxKeyLength(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			return parameters.Any() ? parameters.Select(p => p.Key).Max(k => k.Length) : 0;
		}

		public string ProgramName()
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly == null)
			{
				return _targetType.Name.ToLower();
			}
			return new AssemblyName(entryAssembly.FullName).Name;
		}

		private void PrintUsage()
		{
			if (_metadata.IsMulticommand && !IsSubcommandHelpRequested())
			{
				PrintGeneralMulticommandUsage();
			}
			else if (_metadata.IsMulticommand && IsSubcommandHelpRequested())
			{
				PrintSubcommandUsage();
			}
			else
			{
				PrintUsage(_actionMethods[0]);
			}
		}

		private void PrintSubcommandUsage()
		{
			MethodInfo method = _metadata.GetMethodByName(_args[1].ToLower());
			if (method == null)
			{
				PrintGeneralMulticommandUsage();
				throw new NConsolerException("Unknown subcommand \"{0}\"", _args[0].ToLower());
			}
			PrintUsage(method);
		}

		public bool IsSubcommandHelpRequested()
		{
			return _args.Length > 0
			       && _args[0].ToLower() == "help"
			       && _args.Length == 2;
		}

		private void PrintGeneralMulticommandUsage()
		{
			_messenger.Write(String.Format("usage: {0} <subcommand> [args]", ProgramName()));
			_messenger.Write(String.Format("Type '{0} help <subcommand>' for help on a specific subcommand.", ProgramName()));
			_messenger.Write(String.Empty);
			_messenger.Write("Available subcommands:");

			foreach (MethodInfo method in _actionMethods)
			{
				_messenger.Write(method.Name.ToLower() + " " + GetMethodDescription(method));
			}
		}

		private string GetDisplayName(ParameterInfo parameter)
		{
			if (_metadata.IsRequired(parameter))
			{
				return parameter.Name;
			}
			var optional = _metadata.GetOptional(parameter);
			string parameterName =
				(optional.AltNames.Length > 0) ? optional.AltNames[0] : parameter.Name;
			if (parameter.ParameterType != typeof (bool))
			{
				parameterName += ":" + ValueDescription(parameter.ParameterType);
			}
			return "[/" + parameterName + "]";
		}

		public string ValueDescription(Type type)
		{
			if (type == typeof (int))
			{
				return "number";
			}
			if (type == typeof (string))
			{
				return "value";
			}
			if (type == typeof (int[]))
			{
				return "number[+number]";
			}
			if (type == typeof (string[]))
			{
				return "value[+value]";
			}
			if (type == typeof (DateTime))
			{
				return "dd-mm-yyyy";
			}
			throw new ArgumentOutOfRangeException(String.Format("Type {0} is unknown", type.Name));
		}

		#endregion
	}

	public enum Notation
	{
		None = 0,
		Windows = 1,
		Linux = 2
	}
}

namespace NConsoler
{
	/// <summary>
	/// Used for getting messages from NConsoler
	/// </summary>
	public interface IMessenger
	{
		void Write(string message);
	}
}
namespace NConsoler
{
	using System.Collections.Generic;
	using System.Reflection;

	public interface INotationStrategy
	{
		MethodInfo GetCurrentMethod();
		void ValidateInput(MethodInfo method);
		object[] BuildParameterArray(MethodInfo method);
		IEnumerable<string> OptionalParameters(MethodInfo method);
	}
}
namespace NConsoler
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class LinuxNotationStrategy : INotationStrategy
	{
		private readonly string[] _args;
		private IMessenger _messenger;
		private readonly Metadata _metadata;

		public LinuxNotationStrategy(string[] args, IMessenger messenger, Metadata metadata)
		{
			_args = args;
			_messenger = messenger;
			_metadata = metadata;
		}

		public MethodInfo GetCurrentMethod()
		{
			if (!_metadata.IsMulticommand)
			{
				return _metadata.FirstActionMethod();
			}
			return _metadata.GetMethodByName(_args[0].ToLower());
		}

		public void ValidateInput(MethodInfo method)
		{
		}

		public object[] BuildParameterArray(MethodInfo method)
		{
			var optionalValues = new Dictionary<string, string>();
			for (var i = 0; i < _args.Length - _metadata.RequiredParameterCount(method); i += 2)
			{
				optionalValues.Add(_args[i].Substring(1), _args[i + 1]);
			}
			var parameters = method.GetParameters();
			var parameterValues = parameters.Select(p => (object) null).ToList();

			var requiredStartIndex = _args.Length - _metadata.RequiredParameterCount(method);
			var requiredValues = _args.Where((a, i) => i >= requiredStartIndex).ToList();
			for (var i = 0; i < requiredValues.Count; i++)
			{
				parameterValues[i] = StringToObject.ConvertValue(requiredValues[i], parameters[i].ParameterType);
			}
			for (var i = _metadata.RequiredParameterCount(method); i < parameters.Length; i++ )
			{
				var optional = _metadata.GetOptional(parameters[i]);
				if (optionalValues.ContainsKey(parameters[i].Name))
				{
					parameterValues[i] = StringToObject.ConvertValue(optionalValues[parameters[i].Name], parameters[i].ParameterType);
				}
				else
				{
					parameterValues[i] = optional.Default;
				}
			}
			return parameterValues.ToArray();
		}

		public IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			return new string[] {};
		}
	}
}
namespace NConsoler
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class Metadata
	{
		private readonly IList<MethodInfo> _actionMethods;

		public Metadata(IList<MethodInfo> actionMethods)
		{
			_actionMethods = actionMethods;
		}

		public bool IsMulticommand
		{
			get { return _actionMethods.Count > 1; }
		}

		public bool SingleActionWithOnlyOptionalParametersSpecified()
		{
			if (IsMulticommand) return false;
			MethodInfo method = _actionMethods[0];
			return OnlyOptionalParametersSpecified(method);
		}

		private bool OnlyOptionalParametersSpecified(MethodBase method)
		{
			return method.GetParameters().All(parameter => !IsRequired(parameter));
		}

		public bool IsRequired(ParameterInfo info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
			return !info.IsOptional && (attributes.Length == 0 || attributes[0].GetType() == typeof(RequiredAttribute));
		}

		public bool IsOptional(ParameterInfo info)
		{
			return !IsRequired(info);
		}

		public Consolery.OptionalData GetOptional(ParameterInfo info)
		{
			if (info.IsOptional)
			{
				return new Consolery.OptionalData { Default = info.DefaultValue };
			}
			object[] attributes = info.GetCustomAttributes(typeof(OptionalAttribute), false);
			var attribute = (OptionalAttribute)attributes[0];
			return new Consolery.OptionalData { AltNames = attribute.AltNames, Default = attribute.Default };
		}

		public int RequiredParameterCount(MethodInfo method)
		{
			return method.GetParameters().Count(IsRequired);
		}

		public MethodInfo GetMethodByName(string name)
		{
			return _actionMethods.FirstOrDefault(method => method.Name.ToLower() == name);
		}

		public MethodInfo FirstActionMethod()
		{
			return _actionMethods.FirstOrDefault();
		}
	}
}
namespace NConsoler
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using NConsoler.Extensions;

	public class MetadataValidator
	{
		private readonly Type _targetType;
		private readonly IList<MethodInfo> _actionMethods;
		private readonly Metadata _metadata;

		public MetadataValidator(Type targetType, IList<MethodInfo> actionMethods, Metadata metadata)
		{
			_targetType = targetType;
			_actionMethods = actionMethods;
			_metadata = metadata;
		}

		public void ValidateMetadata()
		{
			CheckAnyActionMethodExists();
			IfActionMethodIsSingleCheckMethodHasParameters();

			foreach (var method in _actionMethods)
			{
				CheckActionMethodNamesAreNotReserved(method);
				CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(method);
				CheckOptionalParametersAreAfterRequiredOnes(method);
				CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(method);
				CheckOptionalParametersAltNamesAreNotDuplicated(method);
			}
		}

		private static void CheckActionMethodNamesAreNotReserved(MethodBase method)
		{
			if (method.Name.ToLower() == "help")
			{
				throw new NConsolerException("Method name \"{0}\" is reserved. Please, choose another name", method.Name);
			}
		}

		private void CheckAnyActionMethodExists()
		{
			if (_actionMethods.Count == 0)
			{
				throw new NConsolerException(
					"Can not find any public static method marked with [Action] attribute in type \"{0}\"", _targetType.Name);
			}
		}

		private void IfActionMethodIsSingleCheckMethodHasParameters()
		{
			if (_actionMethods.Count == 1 && _actionMethods[0].GetParameters().Length == 0)
			{
				throw new NConsolerException(
					"[Action] attribute applied once to the method \"{0}\" without parameters. In this case NConsoler should not be used",
					_actionMethods[0].Name);
			}
		}

		private static void CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] attributes = parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
				if (attributes.Length > 1)
				{
					throw new NConsolerException("More than one attribute is applied to the parameter \"{0}\" in the method \"{1}\"",
												 parameter.Name, method.Name);
				}
			}
		}

		private void CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (_metadata.IsRequired(parameter))
				{
					continue;
				}
				var optional = _metadata.GetOptional(parameter);
				if (optional.Default != null && optional.Default.GetType() == typeof(string) &&
					StringToObject.CanBeConvertedToDate(optional.Default.ToString()))
				{
					return;
				}
				if ((optional.Default == null && !parameter.ParameterType.CanBeNull())
					|| (optional.Default != null && !optional.Default.GetType().IsAssignableFrom(parameter.ParameterType)))
				{
					throw new NConsolerException(
						"Default value for an optional parameter \"{0}\" in method \"{1}\" can not be assigned to the parameter",
						parameter.Name, method.Name);
				}
			}
		}

		private void CheckOptionalParametersAreAfterRequiredOnes(MethodBase method)
		{
			bool optionalFound = false;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (_metadata.IsOptional(parameter))
				{
					optionalFound = true;
				}
				else if (optionalFound)
				{
					throw new NConsolerException(
						"It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"",
						method.Name, parameter.Name);
				}
			}
		}

		private void CheckOptionalParametersAltNamesAreNotDuplicated(MethodBase method)
		{
			var parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (_metadata.IsRequired(parameter))
				{
					parameterNames.Add(parameter.Name.ToLower());
				}
				else
				{
					if (parameterNames.Contains(parameter.Name.ToLower()))
					{
						throw new NConsolerException(
							"Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters",
							parameter.Name, method.Name);
					}
					parameterNames.Add(parameter.Name.ToLower());
					var optional = _metadata.GetOptional(parameter);
					foreach (string altName in optional.AltNames)
					{
						if (parameterNames.Contains(altName.ToLower()))
						{
							throw new NConsolerException(
								"Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters",
								altName, method.Name);
						}
						parameterNames.Add(altName.ToLower());
					}
				}
			}
		}
	}
}
namespace NConsoler
{
	using System;

	/// <summary>
	/// Can be used for safe exception throwing - NConsoler will catch the exception
	/// </summary>
	public sealed class NConsolerException : Exception
	{
		public NConsolerException()
		{
		}

		public NConsolerException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public NConsolerException(string message, params string[] arguments)
			: base(String.Format(message, arguments))
		{
		}
	}
}
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
namespace NConsoler
{
	using System;

	/// <summary>
	/// Should not be used directly
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class ParameterAttribute : Attribute
	{
		/// <summary>
		/// Description is used in help message
		/// </summary>
		public string Description { get; set; }

		protected ParameterAttribute()
		{
			Description = String.Empty;
		}
	}
}
namespace NConsoler
{
	/// <summary>
	/// Marks an Action method parameter as required
	/// </summary>
	public sealed class RequiredAttribute : ParameterAttribute
	{
	}
}
namespace NConsoler
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics.Contracts;
	using System.Globalization;

	public static class StringToObject
	{
		public static object ConvertValue(string value, Type argumentType)
		{
			Contract.Requires(value != null);
			Contract.Requires(argumentType != null);

			if (argumentType.IsArray)
			{
				var arrayItemType = argumentType.GetElementType();
				var array = value.Split('+');
				var valuesArray = Array.CreateInstance(arrayItemType, array.Length);
				for (var i = 0; i < array.Length; i++)
				{
					var arrayItem = ConvertSingleValue(array[i], arrayItemType);
					valuesArray.SetValue(arrayItem, i);
				}
				return valuesArray;
			}

			return ConvertSingleValue(value, argumentType);
		}

		private static object ConvertSingleValue(string value, Type argumentType)
		{
			if (value == String.Empty)
			{
				return GetDefault(argumentType);
			}

			if (IsNullableType(argumentType))
			{
				argumentType = Nullable.GetUnderlyingType(argumentType);
			}

			if (argumentType == typeof (String))
			{
				return value;
			}

			if (argumentType == typeof (DateTime))
			{
				return ConvertToDateTime(value);
			}

			// The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32,
			// UInt32, Int64, UInt64, Char, Double, and Single
			if (argumentType.IsPrimitive || argumentType == typeof (decimal) || argumentType.IsEnum)
			{
				try
				{
					var converter = TypeDescriptor.GetConverter(argumentType);
					// TODO: add possibility to use non invariant cultures
					return converter.ConvertFromString(null, CultureInfo.InvariantCulture, value);
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
			Contract.Requires(parameter != null);

			string[] parts = parameter.Split('-');
			if (parts.Length != 3)
			{
				throw new NConsolerException("Could not convert {0} to Date", parameter);
			}
			var day = (int) ConvertValue(parts[0], typeof (int));
			var month = (int) ConvertValue(parts[1], typeof (int));
			var year = (int) ConvertValue(parts[2], typeof (int));
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
			Contract.Requires(parameter != null);

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

		private static bool IsNullableType(Type type)
		{
			Contract.Requires(type != null);

			return Nullable.GetUnderlyingType(type) != null;
		}

		public static object GetDefault(Type type)
		{
			Contract.Requires(type != null);

			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}
	}
}
namespace NConsoler
{
	using System.Collections.Generic;
	using System.Reflection;

	public class WindowsNotationStrategy : INotationStrategy
	{
		private readonly string[] _args;
		private readonly IMessenger _messenger;
		private readonly Metadata _metadata;

		public WindowsNotationStrategy(string[] args, IMessenger messenger, Metadata metadata)
		{
			_args = args;
			_messenger = messenger;
			_metadata = metadata;
		}

		public MethodInfo GetCurrentMethod()
		{
			if (!_metadata.IsMulticommand)
			{
				return _metadata.FirstActionMethod();
			}
			return _metadata.GetMethodByName(_args[0].ToLower());
		}

		public void ValidateInput(MethodInfo method)
		{
			CheckAllRequiredParametersAreSet(method);
			CheckOptionalParametersAreNotDuplicated(method);
			CheckUnknownParametersAreNotPassed(method);
		}

		private void CheckAllRequiredParametersAreSet(MethodInfo method)
		{
			int minimumArgsLengh = _metadata.RequiredParameterCount(method);
			if (_metadata.IsMulticommand)
			{
				minimumArgsLengh++;
			}
			if (_args.Length < minimumArgsLengh)
			{
				throw new NConsolerException("Not all required parameters are set");
			}
		}

		private void CheckOptionalParametersAreNotDuplicated(MethodInfo method)
		{
			var passedParameters = new List<string>();
			foreach (string optionalParameter in OptionalParameters(method))
			{
				if (!optionalParameter.StartsWith("/"))
				{
					throw new NConsolerException("Unknown parameter {0}", optionalParameter);
				}
				string name = ParameterName(optionalParameter);
				if (passedParameters.Contains(name))
				{
					throw new NConsolerException("Parameter with name {0} passed two times", name);
				}
				passedParameters.Add(name);
			}
		}

		private void CheckUnknownParametersAreNotPassed(MethodInfo method)
		{
			var parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (_metadata.IsRequired(parameter))
				{
					continue;
				}
				parameterNames.Add(parameter.Name.ToLower());
				var optional = _metadata.GetOptional(parameter);
				foreach (string altName in optional.AltNames)
				{
					parameterNames.Add(altName.ToLower());
				}
			}
			foreach (string optionalParameter in OptionalParameters(method))
			{
				string name = ParameterName(optionalParameter);
				if (!parameterNames.Contains(name.ToLower()))
				{
					throw new NConsolerException("Unknown parameter name {0}", optionalParameter);
				}
			}
		}

		private static string ParameterName(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return parameter.Substring(2).ToLower();
			}
			if (parameter.Contains(":"))
			{
				return parameter.Substring(1, parameter.IndexOf(":") - 1).ToLower();
			}
			return parameter.Substring(1).ToLower();
		}

		public object[] BuildParameterArray(MethodInfo method)
		{
			int argumentIndex = _metadata.IsMulticommand ? 1 : 0;
			var parameterValues = new List<object>();
			var aliases = new Dictionary<string, Consolery.ParameterData>();
			foreach (ParameterInfo info in method.GetParameters())
			{
				if (_metadata.IsRequired(info))
				{
					parameterValues.Add(StringToObject.ConvertValue(_args[argumentIndex], info.ParameterType));
				}
				else
				{
					var optional = _metadata.GetOptional(info);

					foreach (string altName in optional.AltNames)
					{
						aliases.Add(altName.ToLower(),
									new Consolery.ParameterData(parameterValues.Count, info.ParameterType));
					}
					aliases.Add(info.Name.ToLower(),
								new Consolery.ParameterData(parameterValues.Count, info.ParameterType));
					parameterValues.Add(optional.Default);
				}
				argumentIndex++;
			}
			foreach (string optionalParameter in OptionalParameters(method))
			{
				string name = ParameterName(optionalParameter);
				string value = ParameterValue(optionalParameter);
				parameterValues[aliases[name].Position] = StringToObject.ConvertValue(value, aliases[name].Type);
			}
			return parameterValues.ToArray();
		}

		private static string ParameterValue(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return "false";
			}
			if (parameter.Contains(":"))
			{
				return parameter.Substring(parameter.IndexOf(":") + 1);
			}
			return "true";
		}

		public IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			int firstOptionalParameterIndex = _metadata.RequiredParameterCount(method);
			if (_metadata.IsMulticommand)
			{
				firstOptionalParameterIndex++;
			}
			for (int i = firstOptionalParameterIndex; i < _args.Length; i++)
			{
				yield return _args[i];
			}
		}
	}
}
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

