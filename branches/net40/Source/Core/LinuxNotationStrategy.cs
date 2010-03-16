using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NConsoler
{
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
			var parameters = method.GetParameters();
			return _args.Select((t, i) => StringToObject.ConvertValue(t, parameters[i].ParameterType)).ToArray();
		}

		public IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			return new string[] {};
		}
	}
}