using Machine.Specifications;

namespace NConsoler.Specs
{
	[Subject("Parameters")]
	public class when_program_contains_only_one_parameter
	{
		Establish context = () => 
			OneParameterProgram.RunCount = 0;

		Because of = () =>
			Consolery.Run(typeof(OneParameterProgram), new[] { "parameter" });

		It should_run_program = () =>
			OneParameterProgram.RunCount.ShouldEqual(1);
	}

	public class OneParameterProgram
	{
		public static int RunCount;

		[Action]
		public static void RunProgram([Required] string parameter)
		{
			RunCount++;
		}
	}

	[Subject("Parameters")]
	public class when_only_optional_parameters_specified
	{
		Establish context = () =>
			OnlyOptionalParametersProgram.RunCount = 0;

		Because of = () =>
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new string[] { });

		It should_run_program = () =>
			OnlyOptionalParametersProgram.RunCount.ShouldEqual(1);
	}

	public class OnlyOptionalParametersProgram
	{
		public static int RunCount;

		[Action]
		public static void RunProgram([Optional(true)]bool parameter)
		{
			RunCount++;
		}
	}

	[Subject("Parameters")]
	public class when_specified_case_for_optional_argument_inconsistent_with_actual_parameters
	{
		Establish context = () =>
			OnlyOptionalParametersProgram.RunCount = 0;

		Because of = () =>
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new[] { "/-PARAMETER" });

		It should_run_program = () =>
			OnlyOptionalParametersProgram.RunCount.ShouldEqual(1);
	}
}