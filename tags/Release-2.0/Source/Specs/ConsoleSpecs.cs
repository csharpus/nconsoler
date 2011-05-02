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
			throw new SpecificException();
		}
	}

	public class SpecificException : Exception
	{
		public SpecificException() {}
	}

	[Subject("Console")]
	public class when_metadata_validation_fails
	{
		Because of = () =>
			Consolery.Run(typeof(ExceptionalProgram), new[] { "wrong" });

		It should_set_error_code = () =>
			Environment.ExitCode.ShouldEqual(1);
	}

	[Subject("Console")]
	public class when_target_method_throws_an_exception
	{
		Because of = () =>
			Exception = Catch.Exception(() => Consolery.Run(typeof(ExceptionalProgram), new[] { "/-parameter" }));

		It should_not_swallow_exception = () =>
			Exception.ShouldNotBeNull();

		It should_not_rethrow_nconsoler_exception = () =>
			Exception.ShouldBe(typeof(SpecificException));

		static Exception Exception;
	}
}
