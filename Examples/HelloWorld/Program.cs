using System;
using NConsoler;

class Program
{
	static void Main(string[] args)
	{
		Consolery.Run(typeof(Program), args);
	}

	[Action]
	public static void ShowMessage(string message)
	{
		Console.WriteLine(message);
	}
}
