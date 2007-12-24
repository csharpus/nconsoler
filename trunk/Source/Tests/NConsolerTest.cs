﻿using System;
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
			messenger.Write("Can not find any public static method in type \"WithoutMethodsProgram\" marked with [Action] attribute");
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
			messenger.Write("More than one attribute is applied to parameter \"parameter\" in method \"RunProgram\"");
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

		[TearDown]
		public void Teardown()
		{
			mocks.VerifyAll();
		}
	}
}
