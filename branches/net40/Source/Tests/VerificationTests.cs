using System;
using System.ComponentModel;
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

		[Test]
		public void should_obtain_information_whether_the_type_is_primitive()
		{
			Assert.That(typeof (int).IsPrimitive, Is.True);
			Assert.That(typeof (double).IsPrimitive, Is.True);
			Assert.That(typeof (string).IsPrimitive, Is.False);
			Assert.That(typeof (char).IsPrimitive, Is.True);
			Assert.That(typeof (decimal).IsPrimitive, Is.False);

			Assert.That(typeof (SomeEnum).IsPrimitive, Is.False);
			Assert.That(typeof (DateTime).IsPrimitive, Is.False);
			Assert.That(typeof (Enum).IsPrimitive, Is.False);
			Assert.That(typeof (ValueType).IsPrimitive, Is.False);
			Assert.That(typeof (int[]).IsPrimitive, Is.False);
			Assert.That(typeof (SomeStruct).IsPrimitive, Is.False);
		}

		public enum SomeEnum
		{
			FirstItem,
			SecondItem
		}

		public struct SomeStruct
		{
		}

		[Test]
		public void should_obtain_information_about_nullable_underlying_type()
		{
			var nullableType = typeof (int?);
			Assert.That(nullableType.IsGenericType, Is.True);
			Assert.That(nullableType.GetGenericTypeDefinition(), Is.EqualTo(typeof (Nullable<>)));
			Assert.That(Nullable.GetUnderlyingType(nullableType), Is.EqualTo(typeof (int)));
		}

		[Test]
		public void should_obtain_infromation_about_array_elements_type()
		{
			var arrayType = typeof (int[]);
			Assert.That(arrayType.IsArray, Is.True);
			Assert.That(arrayType.GetElementType(), Is.EqualTo(typeof (int)));
		}

		[Test]
		public void should_obtain_information_about_enum_items()
		{
			var enumType = typeof (SomeEnum);
			Assert.That(enumType.IsEnum, Is.True);
			Assert.That(enumType.GetEnumNames().First(), Is.EqualTo("FirstItem"));
			Assert.That(Enum.Parse(enumType, "FirstItem"), Is.EqualTo(SomeEnum.FirstItem));
		}

		[Test]
		public void should_convert_values_from_string_using_type_descriptor()
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeof (int));
			Assert.That(converter.ConvertFrom("10"), Is.EqualTo(10));
		}

		[Test]
		[Ignore("It throws System.Exception with OverflowException inside")]
		[ExpectedException(typeof(System.OverflowException))]
		public void should_throw_overflow_exception_for_big_integer()
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(int));
			const long bigValue = (long)Int32.MaxValue + 1;
			converter.ConvertFrom(bigValue.ToString());
		}
	}
}