using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace NConsoler.Tests
{
	[TestFixture]
	public class NConsolerTest
	{
		MockRepository mocks;
		static IMessenger messenger;

		[SetUp]
		public void Setup()
		{
			mocks = new MockRepository();
			messenger = mocks.CreateMock<IMessenger>();
		}

		[Test]
		public void OneParameter()
		{
			messenger.Write("parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram), new string[] { "parameter" });
		}

		public class OneParameterProgram
		{
			[Action]
			public static void RunProgram([Required]string parameter)
			{
				messenger.Write(parameter);
			}
		}

		[Test]
		public void Should_run_program_when_only_optional_parameters_specified()
		{
			messenger.Write("True");
			mocks.ReplayAll();
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new string[] { });
		}

		public class OnlyOptionalParametersProgram
		{
			[Action]
			public static void RunProgram([Optional(true)]bool parameter)
			{
				messenger.Write(parameter.ToString());
			}
		}

		[Test]
		public void Should_run_program_when_specified_case_for_optional_argument_inconsistent_with_actual_parameters()
		{
			messenger.Write("False");
			mocks.ReplayAll();
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new string[] { "/-PARAMETER" });
		}

		[Test]
		public void ManyParameters()
		{
			messenger.Write("string 1 True string 1 True");
			mocks.ReplayAll();
			Consolery.Run(typeof(ManyParametersProgram), 
				new string[] { "string", "1", "true", "/os:string", "/oi:1", "/ob" });
		}

		[Test]
		public void RunConsoleProgramWithoutOptionalParameters()
		{
			messenger.Write("string 1 True 0 0 False");
			mocks.ReplayAll();
			Consolery.Run(typeof(ManyParametersProgram),
				new string[] { "string", "1", "true" });
		}

		[Test]
		public void NegativeBooleanParameter()
		{
			messenger.Write("string 1 True 0 0 False");
			mocks.ReplayAll();
			Consolery.Run(typeof(ManyParametersProgram),
				new string[] { "string", "1", "true", "/-ob" });
		}

		public class ManyParametersProgram
		{
			[Action]
			public static void RunProgram(
				[Required]
				string sParameter,
				int iParameter,
				[Required]
				bool bParameter,
				[Optional("0", "os")]
				string osParameter,
				[Optional(0, "oi")]
				int oiParameter,
				[Optional(false, "ob")]
				bool obParameter)
			{
				messenger.Write(
					String.Format("{0} {1} {2} {3} {4} {5}", 
						sParameter, iParameter, bParameter, osParameter, oiParameter, obParameter));
			}
		}

		[Test]
		public void WithoutMethods()
		{
			messenger.Write("Can not find any public static method marked with [Action] attribute in type \"WithoutMethodsProgram\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(WithoutMethodsProgram), new string[] { "string" }, messenger);
		}

		public class WithoutMethodsProgram
		{
		}

		[Test]
		public void WrongParameterOrder()
		{
			messenger.Write("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"RunProgram\" parameter \"requiredParameter\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(WrongParameterOrderProgram), 
				new string[] { "string" }, messenger);
		}

		public class WrongParameterOrderProgram
		{
			[Action]
			public static void RunProgram(
				[Optional("0")]string optionalParameter,
				[Required]string requiredParameter)
			{

			}
		}

		[Test]
		public void WrongDefaultValueForOptionalStringParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(WrongDefaultValueForOptionalStringParameterProgram),
				new string[] { }, messenger);
		}

		public class WrongDefaultValueForOptionalStringParameterProgram
		{
			[Action]
			public static void RunProgram(
				[Optional(10)]string optionalParameter)
			{

			}
		}

		[Test]
		public void WrongDefaultValueForOptionalIntegerParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(WrongDefaultValueForOptionalIntegerParameterProgram),
				new string[] { }, messenger);
		}

		public class WrongDefaultValueForOptionalIntegerParameterProgram
		{
			[Action]
			public static void RunProgram(
				[Optional("test")]int optionalParameter)
			{

			}
		}

		[Test]
		public void VeryBigDefaultValueForOptionalIntegerParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(VeryBigDefaultValueForOptionalIntegerParameterProgram),
				new string[] { }, messenger);
		}

		public class VeryBigDefaultValueForOptionalIntegerParameterProgram
		{
			[Action]
			public static void RunProgram(
				[Optional("1234567890")]int optionalParameter)
			{

			}
		}

		[Test]
		public void DuplicatedParameterNames()
		{
			messenger.Write("Found duplicated parameter name \"a\" in method \"RunProgram\". Please check alt names for optional parameters");
			mocks.ReplayAll();
			Consolery.Run(typeof(DuplicatedParameterNamesProgram),
				new string[] { }, messenger);
		}

		public class DuplicatedParameterNamesProgram
		{
			[Action]
			public static void RunProgram(
				[Optional(1, "a")]int optionalParameter1,
				[Optional(2, "a")]int optionalParameter2)
			{

			}
		}

		[Test]
		public void DuplicatedParameterAttributes()
		{
			messenger.Write("More than one attribute is applied to the parameter \"parameter\" in the method \"RunProgram\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(DuplicatedParameterAttributesProgram),
				new string[] { "parameter" }, messenger);
		}

		public class DuplicatedParameterAttributesProgram
		{
			[Action]
			public static void RunProgram(
				[Required][Optional("")]string parameter)
			{
			}
		}

		[Test]
		public void NotAllRequiredParametersIsSet()
		{
			messenger.Write("Not all required parameters are set");
			mocks.ReplayAll();
			Consolery.Run(typeof(ManyParametersProgram),
				new string[] { "test" }, messenger);
		}

		[Test]
		public void UnknownParameter()
		{
			messenger.Write("Unknown parameter name /unknown:value");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram),
				new string[] { "required", "/unknown:value" }, messenger);
		}

		[Test]
		public void UnknownBooleanParameterWithNegativeSign()
		{
			messenger.Write("Unknown parameter name /-unknown");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram),
				new string[] { "required", "/-unknown" }, messenger);
		}

		[Test]
		public void UnknownBooleanParameter()
		{
			messenger.Write("Unknown parameter name /unknown");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram),
				new string[] { "required", "/unknown" }, messenger);
		}

		[Test]
		public void Should_choose_correct_method_if_more_than_one_method()
		{
			messenger.Write("m2test");
			mocks.ReplayAll();
			Consolery.Run(typeof(TwoActionsProgram),
				new string[] { "Test2", "test" }, messenger);
		}

		[Test]
		public void Should_show_help_for_a_particular_message()
		{
			messenger.Write("usage: twoactionsprogram test2 parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(TwoActionsProgram), new string[] { "help", "Test2" }, messenger);
		}

		public class TwoActionsProgram
		{
			[Action]
			public static void Test1(
				[Required]string parameter)
			{
				messenger.Write("m1" + parameter);
			}

			[Action]
			public static void Test2(
				[Required]string parameter)
			{
				messenger.Write("m2" + parameter);
			}
		}

		[Test]
		public void Should_work_without_arguments_in_action()
		{
			messenger.Write("test");
			mocks.ReplayAll();
			Consolery.Run(typeof(WithoutArgumentsProgram),
				new string[] { "Test" }, messenger);
		}

		public class WithoutArgumentsProgram
		{
			[Action]
			public static void Test()
			{
				messenger.Write("test");
			}

			[Action]
			public static void Test1()
			{
				messenger.Write("test1");
			}
		}

		[Test]
		public void Should_write_exeption_message()
		{
			messenger.Write("Incorrect arguments!");
			mocks.ReplayAll();
			Consolery.Run(typeof(ThrowExceptionProgram), new string[] {"value"}, messenger);
		}

		public class ThrowExceptionProgram
		{
			[Action]
			public static void Test(string argument)
			{
				throw new ArgumentException("Incorrect arguments!");
			}
		}

		public class OptionalDateTimeProgram
		{
			[Action]
			public static void Test(DateTime required, [Optional("31-12-2008", "dtDate")]DateTime optional)
			{
				messenger.Write(optional.ToString("dd-MM-yyyy"));
			}
		}

		[Test]
		public void Should_correctly_convert_to_datetime_from_optional_attribute_default_value()
		{
			messenger.Write("31-12-2008");
			mocks.ReplayAll();
			Consolery.Run(typeof(OptionalDateTimeProgram), new string[] { "01-01-2009", "/dtDate:31-12-2008" }, messenger);
		}

		[TearDown]
		public void Teardown()
		{
			mocks.VerifyAll();
		}
	}
}
