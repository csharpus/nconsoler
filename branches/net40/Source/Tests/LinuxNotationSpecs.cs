using NUnit.Framework;
using Rhino.Mocks;

namespace NConsoler.Tests
{
	[TestFixture]
	public class when_only_required_params_specified
	{
		[Test]
		public void should_run_action_method()
		{
			var messenger = MockRepository.GenerateStub<IMessenger>();
			Consolery.Run(typeof(Only_required_params), new string[] { "v" }, messenger, Notation.Linux);
		}

		public class Only_required_params
		{
			[Action]
			public static void Method(string value)
			{
				
			}
		}
	}
}