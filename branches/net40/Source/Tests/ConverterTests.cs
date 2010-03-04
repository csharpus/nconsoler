using NUnit.Framework;

namespace NConsoler.Tests
{
	[TestFixture]
	public class ConverterTests
	{
		[Test]
		public void TestDecimalParameter()
		{
			var result = (decimal)StringToObject.ConvertValue("10,00", typeof (decimal));
			Assert.That(result == 10.00m);
		}

		[Test]
		public void TestStringParameter()
		{
			var result = (string)StringToObject.ConvertValue("test", typeof(string));
			Assert.That(result == "test");
		}

		[Test]
		public void TestIntegerParameter()
		{
			var result = (int)StringToObject.ConvertValue("11", typeof(int));
			Assert.That(result == 11);
		}

		[Test]
		public void TestDoubleParameter()
		{
			var result = (double)StringToObject.ConvertValue("11,11", typeof(double));
			Assert.That(result == 11.11d);
		}
	}
}
