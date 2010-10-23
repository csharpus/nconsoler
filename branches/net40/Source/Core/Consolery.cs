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