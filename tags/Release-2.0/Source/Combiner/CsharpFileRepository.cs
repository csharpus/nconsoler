using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Combiner
{
	public class CsharpFileRepository
	{
		private readonly string _rootFolder;

		public CsharpFileRepository(string rootFolder)
		{
			_rootFolder = rootFolder;
		}

		public IList<string> Search()
		{
			return new DirectoryInfo(_rootFolder)
				.GetFiles("*.cs", SearchOption.AllDirectories)
				.Select(f => Path.Combine(f.Directory.FullName, f.Name)).ToList();
		}
	}
}