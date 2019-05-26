using System;
using NConsoler;

class Program
{
	static void Main(string[] args)
	{
		Consolery.Run(typeof(Program), args);
	}

	[Action]
	public static void Multiple(
		[Required(Description = "1st multiplier")]
		int factor1,

		[Required(Description = "2nd multiplier")]
		int factor2,

		[Optional(true, Description = "Show program logo")]
		bool showlogo)
	{
		if (showlogo)
		{
			Console.WriteLine("Multiplier example");
		}
		Console.WriteLine(factor1 * factor2);
	}
}