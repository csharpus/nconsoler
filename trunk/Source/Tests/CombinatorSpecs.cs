using System.Collections.Generic;
using Combiner;
using NUnit.Framework;

namespace NConsoler.Specs.Combiner
{
	[TestFixture]
	public class CombinatorTests
	{
		[Test]
		public void should_return_whole_content()
		{
			var files = new List<string>
			            	{
			            		"using System;", 
								"using System.Text;"
			            	};

			var result = new CsharpCombinator().Combine(files);

			Assert.That(result, Is.EqualTo("using System;\r\nusing System.Text;\r\n"));
		}
	}
}