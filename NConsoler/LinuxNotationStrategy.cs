namespace NConsoler
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class LinuxNotationStrategy : INotationStrategy
    {
        private readonly string[] args;
        private readonly IMessenger messenger;
        private readonly Metadata metadata;

        public LinuxNotationStrategy(string[] args, IMessenger messenger, Metadata metadata)
        {
            this.args = args;
            this.messenger = messenger;
            this.metadata = metadata;
        }

        public MethodInfo GetCurrentMethod()
        {
            if (!this.metadata.IsMulticommand)
            {
                return this.metadata.FirstActionMethod();
            }
            return this.metadata.GetMethodByName(this.args[0].ToLower());
        }

        public void ValidateInput(MethodInfo method)
        {
        }

        public object[] BuildParameterArray(MethodInfo method)
        {
            var optionalValues = new Dictionary<string, string>();
            for (var i = 0; i < this.args.Length - this.metadata.RequiredParameterCount(method); i += 2)
            {
                optionalValues.Add(this.args[i].Substring(1), this.args[i + 1]);
            }
            var parameters = method.GetParameters();
            var parameterValues = parameters.Select(p => (object)null).ToList();

            var requiredStartIndex = this.args.Length - this.metadata.RequiredParameterCount(method);
            var requiredValues = this.args.Where((a, i) => i >= requiredStartIndex).ToList();
            for (var i = 0; i < requiredValues.Count; i++)
            {
                parameterValues[i] = StringToObject.ConvertValue(requiredValues[i], parameters[i].ParameterType);
            }
            for (var i = this.metadata.RequiredParameterCount(method); i < parameters.Length; i++)
            {
                var optional = this.metadata.GetOptional(parameters[i]);
                if (optionalValues.ContainsKey(parameters[i].Name))
                {
                    parameterValues[i] = StringToObject.ConvertValue(optionalValues[parameters[i].Name], parameters[i].ParameterType);
                }
                else
                {
                    parameterValues[i] = optional.Default;
                }
            }
            return parameterValues.ToArray();
        }

        public IEnumerable<string> OptionalParameters(MethodInfo method) => new string[] { };

        public void PrintUsage() => throw new System.NotImplementedException();
    }
}