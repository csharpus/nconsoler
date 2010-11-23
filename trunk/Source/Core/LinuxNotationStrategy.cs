namespace NConsoler
{
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