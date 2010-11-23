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