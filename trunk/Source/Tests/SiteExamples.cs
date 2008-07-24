// <examples>
namespace NConsoler.Tests
{
    using System;
    using NUnit.Framework;
    using Rhino.Mocks;

	[TestFixture]
	public class SiteExamples
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
		public void HelloWorldExampleFromQuickStart()
		{
			HelloWorldExample.Main(new string[] { "Hello World" });
		}

		class HelloWorldExample
		{
			public static void Main(string[] args)
			{
				Consolery.Run(typeof(HelloWorldExample), args);
			}

			[Action]
			public static void ShowMessage(string message)
			{
				Console.WriteLine(message);
			}
		}

        [Test]
        public void ParametersExample()
        {
            using (mocks.Record())
            {
            }
            using (mocks.Playback())
            {
                Consolery.Run(
                    typeof (Parameters),
                    new string[] {"first", "2", @"a+b", "4+5+6", "20-01-2008", "/flag"},
                    messenger);
            }
        }

        class Parameters
        {
// <example name="parameters">
            [Action]
            public static void Method(
                [Required]
                string p1,
                [Required]
                int p2,
                [Required]
                string[] p3,
                [Required]
                int[] p4,
                [Required]
                DateTime p5,
                [Optional(false)]
                bool flag)
// </example>
            {
            }
        }

        [Test]
        public void CheatSheetExamples()
        {
			using (mocks.Record())
			{
			}
			using (mocks.Playback())
			{
				Consolery.Run(
					typeof(CheatSheet),
					new string[] { "first", "/optional:2", @"/value:3" },
					messenger);
			} 
        }

        class CheatSheet {
// <example name="action_method_cheath_sheet">
			[Action]
			public static void Method(
// </example>
// <example name="required_parameter_cheath_sheet">
				[Required(Description = "Parameter name")]
				string name,
// </example>
// <example name="optional_parameter_cheath_sheet">
				[Optional("Parameter name")]
				string optional,
// </example>
// <example name="alias_cheath_sheet">
				[Optional("Default value", "anAlias")]
				string value
// </example>
			)
			{
			}
		}

		[Test]
		public void MultipleActionsExample()
		{
			using (mocks.Record())
			{
			}
			using (mocks.Playback())
			{
				Consolery.Run(
					typeof(MultipleExamples),
					new string[] { "add", "Bender" },
					messenger);
			} 
		}

		class MultipleExamples
		{
// <example name="multiple_actions">
			[Action]
			public static void Add(
				[Required]
				string userName)
			{

			}

			[Action]
			public static void Remove(
				[Required]
				string userName)
			{

			}
// </example>
		}

		[Test]
		public void ValidationExample()
		{
			using (mocks.Record())
			{
				messenger.Write("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"Method\" parameter \"required\"");
			}
			using (mocks.Playback())
			{
				Consolery.Run(
					typeof(Validation),
					new string[] { "add", "Bender" },
					messenger);
			} 
		}

		class Validation
		{
// <example name="validation">
			[Action]
			public static void Method(
				[Optional("default")]
				string optional,
				[Required]
				string required)
// </example>
			{
				
			}
		}

		[Test]
		public void HelpExample()
		{
			using (mocks.Record())
			{
			}
			using (mocks.Playback())
			{
				Consolery.Run(
					typeof(Help),
					new string[] { },
					messenger);
			} 
		}

		class Help
		{
// <example name="help">
			[Action]
			public static void Method(
				[Required(Description = "Applies some magic")]
				string flag)
// </example>
			{
				
			}

		}
	}
}
// </examples>