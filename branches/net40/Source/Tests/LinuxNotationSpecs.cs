using System;
using NUnit.Framework;
using Rhino.Mocks;

namespace NConsoler.Tests
{
	[TestFixture]
	public class when_only_required_params_specified
	{
		[Test]
		public void should_run_action_method_with_only_required_params()
		{
			var messenger = MockRepository.GenerateStub<IMessenger>();
			Consolery.Run(typeof(Only_required_params), new[] { "v" }, messenger, Notation.Linux);
		}

		public class Only_required_params
		{
			[Action]
			public static void Method(string value)
			{
				
			}
		}

		[Test]
		public void should_run_action_method_with_optional_params()
		{
			var messenger = MockRepository.GenerateStub<IMessenger>();
			Consolery.Run(typeof(Required_and_optional_params), new[] { "-optional", "optional_value", "value" }, messenger, Notation.Linux);
		}

		public class Required_and_optional_params
		{
			[Action]
			public static void Method(string value, string optional = "value")
			{
				Console.WriteLine(value + " " + optional);
			}
		}

	}
}