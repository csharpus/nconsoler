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

		[Action(Description = "First method")]
		public static void Method1(
			[Required(Description = "Some message")]
			string message)
		{
		}

		[Action(Description = "Second method")]
		public static void Method2(
			[Required(Description = "Other message")]
			string message)
		{
		}
	}
}
