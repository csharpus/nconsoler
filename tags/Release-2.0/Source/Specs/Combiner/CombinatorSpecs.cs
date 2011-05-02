using System.Collections.Generic;
using Combiner;

using Machine.Specifications;

namespace NConsoler.Specs.Combiner
{
	public class when_combining_two_cs_files
	{
		static string result;
		static List<string> files = new List<string>();

		Establish context = () =>
			{
				files.Add("using System;");
				files.Add("using System.Text;");
			};

		Because of = () => 
			result = new CsharpCombinator().Combine(files);

		It should_return_whole_content = () =>
			result.ShouldEqual("using System;\r\nusing System.Text;\r\n");
	}
}