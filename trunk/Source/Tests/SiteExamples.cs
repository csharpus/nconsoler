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
	}
}
