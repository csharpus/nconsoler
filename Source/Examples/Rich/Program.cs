using System;
using NConsoler;

public class Program
{
	static void Main(string[] args)
	{
		Consolery.Run();
	}

	// consoler.exe 10 "description" /b
	[Action("Deletes some objects")]
	public static void Delete(
		[Required(Description = "Object count")]
		int count,

		[Required(Description = "Object description")]
		string description,

		[Optional(false, "b", "bk", Description = "Boolean value")]
		bool book,

		[Optional("", "c")]
		string comment,

		[Optional(1)]
		int length)
	{
		Console.WriteLine("Delete {0} {1} {2} {3}", count, description, book, comment);
	}
}
