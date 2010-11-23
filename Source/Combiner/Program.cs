using System.IO;
using System.Linq;

namespace Combiner
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var combinator = new CsharpCombinator();
			var repository = new CsharpFileRepository("../../../Core/");

			var files = repository.Search().Where(f => !f.EndsWith("AssemblyInfo.cs")).ToList();
			var filesContent = files.Select(File.ReadAllText).ToList();

			var content = combinator.Combine(filesContent);
			File.WriteAllText("../../NConsoler.cs", content);
		}
	}
}