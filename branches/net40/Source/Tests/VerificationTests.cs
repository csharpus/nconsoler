using System;
using System.Linq;
using NUnit.Framework;

namespace NConsoler.Tests
{
	[TestFixture]
	public class VerificationTests
	{
		[Test]
		public void should_obtain_default_value_for_parameter_information()
		{
			var parameterInfo = typeof (ClassWithOptionalParameters)
				.GetMethods()
				.Where(m => m.Name == "Method")
				.Single()
				.GetParameters()
				.First();
			Assert.That(parameterInfo.IsOptional, Is.True);
			Assert.That(parameterInfo.DefaultValue, Is.EqualTo("test"));
		}

		public class ClassWithOptionalParameters
		{
			public void Method(string value = "test")
			{
				
			}

			public void MethodWithDifferentDefaultValues(
				bool? test = null, 
				DateTime? date = null, 
				long l = Int64.MaxValue,
				object o = null,
				ClassWithOptionalParameters c = null)
			{
				
			}
		}
	}
}
