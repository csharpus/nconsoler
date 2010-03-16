using System;
using System.Collections.Generic;
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
			return new object[] {};
		}

		public IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			return new string[] {};
		}
	}
}