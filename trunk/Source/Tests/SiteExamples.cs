namespace NConsoler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
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
            {
            }
        }
	}
}
