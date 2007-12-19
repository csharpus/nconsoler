using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Consoler
{
	class Program
	{
		static void Main(string[] args)
		{
			Consoler.Run(typeof(Program), args);
		}

		// consoler.exe 10 "description" /b
		[Action]
		public static void Delete(
			[Required(Description = "Object count")]
			int count,

			[Required(Description = "Object description")]
			string description,
			
			[Optional("b", "bk", Default = true, Description = "Boolean value")]
			bool book)
		{
			Console.WriteLine("Delete");
		}
	}
}
