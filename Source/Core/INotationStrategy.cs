using System.Collections.Generic;
using System.Reflection;

namespace NConsoler
{
	public interface INotationStrategy
	{
		MethodInfo GetCurrentMethod();
		void ValidateInput(MethodInfo method);
		object[] BuildParameterArray(MethodInfo method);
		IEnumerable<string> OptionalParameters(MethodInfo method);
	}
}