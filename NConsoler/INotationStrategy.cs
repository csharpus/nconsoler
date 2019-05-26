namespace NConsoler
{
    using System.Collections.Generic;
    using System.Reflection;

    public interface INotationStrategy
    {
        void PrintUsage();
        MethodInfo GetCurrentMethod();
        void ValidateInput(MethodInfo method);
        object[] BuildParameterArray(MethodInfo method);
        IEnumerable<string> OptionalParameters(MethodInfo method);
    }
}