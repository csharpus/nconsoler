using System;
using NConsoler;

class Program
{
	static void Main(string[] args)
	{
		Consolery.Run(typeof(Program), args);
	}

	[Action]
	public static void DoWork(
		  [Required]
          int count,
		  [Optional(false)]
          bool flag)
	{
		Console.WriteLine("DoWork {0} {1}", count, flag);
	}
}
