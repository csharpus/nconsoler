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
		public void RunConsoleProgramWithOneParameter()
		{
			messenger.Write("parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithOneParameter), new string[] { "parameter" });
		}

		public class ConsoleProgramWithOneParameter
		{
			[Action]
			public static void RunProgram([Required]string parameter)
			{
				messenger.Write(parameter);
			}
		}

		[Test]
		public void RunConsoleProgramWithManyParameters()
		{
			messenger.Write("string 1 True string 1 True");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithManyParameters), 
				new string[] { "string", "1", "true", "/os:string", "/oi:1", "/ob" });
		}

		[Test]
		public void RunConsoleProgramWithoutOptionalParameters()
		{
			messenger.Write("string 1 True 0 0 False");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithManyParameters),
				new string[] { "string", "1", "true" });
		}

		[Test]
		public void RunConsoleProgramWithNegativeBooleanParameter()
		{
			messenger.Write("string 1 True 0 0 False");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithManyParameters),
				new string[] { "string", "1", "true", "/-ob" });
		}

		public class ConsoleProgramWithManyParameters
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
		public void RunConsoleProgramWithoutMethods()
		{
			messenger.Write("string 1 True string 1 True");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithoutMethods), new string[] { "string" }, messenger);
		}

		public class ConsoleProgramWithoutMethods
		{
		}

		[Test]
		public void RunConsoleProgramWithWrongParameterOrder()
		{
			messenger.Write("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"RunProgram\" parameter \"requiredParameter\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithWrongParameterOrder), 
				new string[] { "string" }, messenger);
		}

		public class ConsoleProgramWithWrongParameterOrder
		{
			[Action]
			public static void RunProgram(
				[Optional("0")]string optionalParameter,
				[Required]string requiredParameter)
			{

			}
		}

		[Test]
		public void RunConsoleProgramWithWrongDefaultValueForOptionalStringParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithWrongDefaultValueForOptionalStringParameter),
				new string[] { }, messenger);
		}

		public class ConsoleProgramWithWrongDefaultValueForOptionalStringParameter
		{
			[Action]
			public static void RunProgram(
				[Optional(10)]string optionalParameter)
			{

			}
		}

		[Test]
		public void RunConsoleProgramWithWrongDefaultValueForOptionalIntegerParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithWrongDefaultValueForOptionalIntegerParameter),
				new string[] { }, messenger);
		}

		public class ConsoleProgramWithWrongDefaultValueForOptionalIntegerParameter
		{
			[Action]
			public static void RunProgram(
				[Optional("test")]int optionalParameter)
			{

			}
		}

		[Test]
		public void RunConsoleProgramWithVeryBigDefaultValueForOptionalIntegerParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithVeryBigDefaultValueForOptionalIntegerParameter),
				new string[] { }, messenger);
		}

		public class ConsoleProgramWithVeryBigDefaultValueForOptionalIntegerParameter
		{
			[Action]
			public static void RunProgram(
				[Optional("1234567890")]int optionalParameter)
			{

			}
		}

		[Test]
		public void RunConsoleProgramWithDuplicatedParameterNames()
		{
			messenger.Write("");
			mocks.ReplayAll();
			Consolery.Run(typeof(ConsoleProgramWithDuplicatedParameterNames),
				new string[] { }, messenger);
		}

		public class ConsoleProgramWithDuplicatedParameterNames
		{
			[Action]
			public static void RunProgram(
				[Optional(1, "a")]int optionalParameter1,
				[Optional(2, "a")]int optionalParameter2)
			{

			}
		}

		[TearDown]
		public void Teardown()
		{
			mocks.VerifyAll();
		}
	}
}
