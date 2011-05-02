using System.Collections.Generic;
using System.Text;

namespace Combiner
{
	public class CsharpCombinator
	{
		public string Combine(List<string> files)
		{
			var builder = new StringBuilder();
			foreach (var file in files)
			{
				builder.AppendLine(file);
			}
			return builder.ToString();
		}
	}
}