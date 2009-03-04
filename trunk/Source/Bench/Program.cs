using System;
using System.Collections.Generic;
using System.Text;
using NConsoler;

namespace Bench
{
	public class Program
	{
		static void Main(string[] args)
		{
			Consolery.Run();
		}

		[Action]
		public static void M1([Optional(true)] bool param)
		{
			Console.WriteLine(param.ToString());
		}

		//[Action(Description = "First method")]
		//public static void Method1(
		//    [Required(Description = "Some message")]
		//    string message)
		//{
		//}

		//[Action(Description = "Second method")]
		//public static void Method2(
		//    [Required(Description = "Other message")]
		//    string message)
		//{
		//}

		//[Action]
		//public static void Help()
		//{
		//}
	}
}
