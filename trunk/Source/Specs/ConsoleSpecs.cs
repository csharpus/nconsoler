using System;
using Machine.Specifications;

namespace NConsoler.Specs
{
	public class ExceptionalProgram
	{
		public static int RunCount;

		[Action]
		public static void RunProgram([Optional(true)]bool parameter)
		{
			throw new Exception();
		}
	}

	[Subject("Parameters")]
	public class when_running_method_throws_an_exception
	{
		Because of = () =>
			Consolery.Run(typeof(ExceptionalProgram), new[] { "/-PARAMETER" });

		It should_set_error_code = () =>
			Environment.ExitCode.ShouldEqual(1);
	}
}
