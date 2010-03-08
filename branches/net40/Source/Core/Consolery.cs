//
// NConsoler 1.1
// http://nconsoler.csharpus.com
//

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace NConsoler
{
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
		public static void Run(Type targetType, string[] args, IMessenger messenger)
		{
			try
			{
				new Consolery(targetType, args, messenger).RunAction();
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
			new Consolery(targetType, new string[] {}, new ConsoleMessenger()).ValidateMetadata();
		}

		private readonly Type _targetType;
		private readonly string[] _args;
		private readonly List<MethodInfo> _actionMethods = new List<MethodInfo>();
		private readonly IMessenger _messenger;
		private readonly NotationStrategy _notation;

		public Consolery(Type targetType, string[] args, IMessenger messenger)
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

			_notation = new NotationStrategy(this, _args, _messenger);
		}

		private bool SingleActionWithOnlyOptionalParametersSpecified()
		{
			if (IsMulticommand) return false;
			MethodInfo method = _actionMethods[0];
			return OnlyOptionalParametersSpecified(method);
		}

		private bool OnlyOptionalParametersSpecified(MethodBase method)
		{
			return method.GetParameters().All(parameter => !IsRequired(parameter));
		}

		private void RunAction()
		{
			ValidateMetadata();
			if (IsHelpRequested())
			{
				PrintUsage();
				return;
			}

			MethodInfo currentMethod = GetCurrentMethod();
			if (currentMethod == null)
			{
				PrintUsage();
				throw new NConsolerException("Unknown subcommand \"{0}\"", _args[0]);
			}
			_notation.ValidateInput(currentMethod);
			InvokeMethod(currentMethod);
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

		public bool IsRequired(ParameterInfo info)
		{
			object[] attributes = info.GetCustomAttributes(typeof (ParameterAttribute), false);
			return !info.IsOptional && (attributes.Length == 0 || attributes[0].GetType() == typeof (RequiredAttribute));
		}

		private bool IsOptional(ParameterInfo info)
		{
			return !IsRequired(info);
		}

		public OptionalData GetOptional(ParameterInfo info)
		{
			if (info.IsOptional)
			{
				return new OptionalData {Default = info.DefaultValue};
			}
			object[] attributes = info.GetCustomAttributes(typeof(OptionalAttribute), false);
			var attribute = (OptionalAttribute)attributes[0];
			return new OptionalData {AltNames = attribute.AltNames, Default = attribute.Default};
		}

		public class OptionalData
		{
			public OptionalData()
			{
				AltNames = new string[]{};
			}

			public string[] AltNames { get; set; }

			public object Default { get; set; }
		}

		public bool IsMulticommand
		{
			get { return _actionMethods.Count > 1; }
		}

		private bool IsHelpRequested()
		{
			return (_args.Length == 0 && !SingleActionWithOnlyOptionalParametersSpecified())
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

		public IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			int firstOptionalParameterIndex = RequiredParameterCount(method);
			if (IsMulticommand)
			{
				firstOptionalParameterIndex++;
			}
			for (int i = firstOptionalParameterIndex; i < _args.Length; i++)
			{
				yield return _args[i];
			}
		}

		public int RequiredParameterCount(MethodInfo method)
		{
			return method.GetParameters().Count(IsRequired);
		}

		private MethodInfo GetCurrentMethod()
		{
			if (!IsMulticommand)
			{
				return _actionMethods[0];
			}
			return GetMethodByName(_args[0].ToLower());
		}

		public MethodInfo GetMethodByName(string name)
		{
			return _actionMethods.FirstOrDefault(method => method.Name.ToLower() == name);
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
			string subcommand = IsMulticommand ? method.Name.ToLower() + " " : String.Empty;
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
				return (attribute).Description;
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
			if (IsMulticommand && !IsSubcommandHelpRequested())
			{
				PrintGeneralMulticommandUsage();
			}
			else if (IsMulticommand && IsSubcommandHelpRequested())
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
			MethodInfo method = GetMethodByName(_args[1].ToLower());
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
			if (IsRequired(parameter))
			{
				return parameter.Name;
			}
			var optional = GetOptional(parameter);
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

		#region Validation

		private void ValidateMetadata()
		{
			CheckAnyActionMethodExists();
			IfActionMethodIsSingleCheckMethodHasParameters();
			foreach (MethodInfo method in _actionMethods)
			{
				CheckActionMethodNamesAreNotReserved();
				CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(method);
				CheckOptionalParametersAreAfterRequiredOnes(method);
				CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(method);
				CheckOptionalParametersAltNamesAreNotDuplicated(method);
			}
		}

		private void CheckActionMethodNamesAreNotReserved()
		{
			foreach (MethodInfo method in _actionMethods)
			{
				if (method.Name.ToLower() == "help")
				{
					throw new NConsolerException("Method name \"{0}\" is reserved. Please, choose another name", method.Name);
				}
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
				object[] attributes = parameter.GetCustomAttributes(typeof (ParameterAttribute), false);
				if (attributes.Length > 1)
				{
					throw new NConsolerException("More than one attribute is applied to the parameter \"{0}\" in the method \"{1}\"",
					                             parameter.Name, method.Name);
				}
			}
		}

		private static bool CanBeNull(Type type)
		{
			return type == typeof (string)
			       || type == typeof (string[])
			       || type == typeof (int[]);
		}

		private void CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				var optional = GetOptional(parameter);
				if (optional.Default != null && optional.Default.GetType() == typeof (string) &&
				    StringToObject.CanBeConvertedToDate(optional.Default.ToString()))
				{
					return;
				}
				if ((optional.Default == null && !CanBeNull(parameter.ParameterType))
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
				if (IsOptional(parameter))
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
				if (IsRequired(parameter))
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
					var optional = GetOptional(parameter);
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

		#endregion
	}
}