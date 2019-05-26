namespace NConsoler
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;
    using System.Linq;

    public class WindowsNotationStrategy : INotationStrategy
    {
        private readonly string[] args;
        private readonly IMessenger messenger;
        private readonly Metadata metadata;
        private readonly Type targetType;
        private readonly List<MethodInfo> actionMethods;

        public WindowsNotationStrategy(string[] args, IMessenger messenger, Metadata metadata, Type targetType, List<MethodInfo> actionMethods)
        {
            this.args = args;
            this.messenger = messenger;
            this.metadata = metadata;
            this.targetType = targetType;
            this.actionMethods = actionMethods;
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
            this.CheckAllRequiredParametersAreSet(method);
            this.CheckOptionalParametersAreNotDuplicated(method);
            this.CheckUnknownParametersAreNotPassed(method);
        }

        private void CheckAllRequiredParametersAreSet(MethodInfo method)
        {
            var minimumArgsLengh = this.metadata.RequiredParameterCount(method);
            if (this.metadata.IsMulticommand)
            {
                minimumArgsLengh++;
            }
            if (this.args.Length < minimumArgsLengh)
            {
                this.PrintUsage();
                throw new NConsolerException("Error: Not all required parameters are set");
            }
        }

        private void CheckOptionalParametersAreNotDuplicated(MethodInfo method)
        {
            var passedParameters = new List<string>();
            foreach (var optionalParameter in this.OptionalParameters(method))
            {
                if (!optionalParameter.StartsWith("/"))
                {
                    throw new NConsolerException("Unknown parameter {0}", optionalParameter);
                }
                var name = ParameterName(optionalParameter);
                if (passedParameters.Contains(name))
                {
                    throw new NConsolerException("Parameter with name {0} passed two times", name);
                }
                passedParameters.Add(name);
            }
        }

        private void CheckUnknownParametersAreNotPassed(MethodInfo method)
        {
            var parameterNames = new List<string>();
            foreach (var parameter in method.GetParameters())
            {
                if (this.metadata.IsRequired(parameter))
                {
                    continue;
                }
                parameterNames.Add(parameter.Name.ToLower());
                var optional = this.metadata.GetOptional(parameter);
                foreach (var altName in optional.AltNames)
                {
                    parameterNames.Add(altName.ToLower());
                }
            }
            foreach (var optionalParameter in this.OptionalParameters(method))
            {
                var name = ParameterName(optionalParameter);
                if (!parameterNames.Contains(name.ToLower()))
                {
                    throw new NConsolerException("Unknown parameter name {0}", optionalParameter);
                }
            }
        }

        private static string ParameterName(string parameter)
        {
            if (parameter.StartsWith("/-"))
            {
                return parameter.Substring(2).ToLower();
            }
            if (parameter.Contains(":"))
            {
                return parameter.Substring(1, parameter.IndexOf(":") - 1).ToLower();
            }
            return parameter.Substring(1).ToLower();
        }

        public object[] BuildParameterArray(MethodInfo method)
        {
            var argumentIndex = this.metadata.IsMulticommand ? 1 : 0;
            var parameterValues = new List<object>();
            var aliases = new Dictionary<string, Consolery.ParameterData>();
            foreach (var info in method.GetParameters())
            {
                if (this.metadata.IsRequired(info))
                {
                    parameterValues.Add(StringToObject.ConvertValue(this.args[argumentIndex], info.ParameterType));
                }
                else
                {
                    var optional = this.metadata.GetOptional(info);

                    foreach (var altName in optional.AltNames)
                    {
                        aliases.Add(altName.ToLower(),
                                    new Consolery.ParameterData(parameterValues.Count, info.ParameterType));
                    }
                    aliases.Add(info.Name.ToLower(),
                                new Consolery.ParameterData(parameterValues.Count, info.ParameterType));
                    parameterValues.Add(optional.Default);
                }
                argumentIndex++;
            }
            foreach (var optionalParameter in this.OptionalParameters(method))
            {
                var name = ParameterName(optionalParameter);
                var value = ParameterValue(optionalParameter);
                parameterValues[aliases[name].Position] = StringToObject.ConvertValue(value, aliases[name].Type);
            }
            return parameterValues.ToArray();
        }

        private static string ParameterValue(string parameter)
        {
            if (parameter.StartsWith("/-"))
            {
                return "false";
            }
            if (parameter.Contains(":"))
            {
                return parameter.Substring(parameter.IndexOf(":") + 1);
            }
            return "true";
        }

        public IEnumerable<string> OptionalParameters(MethodInfo method)
        {
            var firstOptionalParameterIndex = this.metadata.RequiredParameterCount(method);
            if (this.metadata.IsMulticommand)
            {
                firstOptionalParameterIndex++;
            }
            for (var i = firstOptionalParameterIndex; i < this.args.Length; i++)
            {
                yield return this.args[i];
            }
        }

        public void PrintUsage()
        {
            if (this.metadata.IsMulticommand && !this.IsSubcommandHelpRequested())
            {
                this.PrintGeneralMulticommandUsage();
            }
            else if (this.metadata.IsMulticommand && this.IsSubcommandHelpRequested())
            {
                this.PrintSubcommandUsage();
            }
            else
            {
                this.PrintUsage(this.actionMethods[0]);
            }
        }

        private void PrintSubcommandUsage()
        {
            var method = this.metadata.GetMethodByName(this.args[1].ToLower());
            if (method == null)
            {
                this.PrintGeneralMulticommandUsage();
                throw new NConsolerException("Unknown subcommand \"{0}\"", this.args[0].ToLower());
            }
            this.PrintUsage(method);
        }

        #region Usage

        private void PrintUsage(MethodInfo method)
        {
            this.PrintMethodDescription(method);
            var parameters = this.GetParametersMetadata(method);
            this.PrintUsageExample(method, parameters);
            this.PrintParameterUsage(parameters);
        }

        private void PrintUsageExample(MethodInfo method, IList<ParameterMetadata> parameterList)
        {
            var subcommand = this.metadata.IsMulticommand ? method.Name.ToLower() + " " : string.Empty;

            var parameters = string.Join(" ", parameterList.Select(p => p.Name).ToArray());
            this.messenger.Write("usage: " + this.ProgramName() + " " + subcommand + parameters);
        }

        private void PrintMethodDescription(MethodInfo method)
        {
            var description = this.GetMethodDescription(method);
            if (description == string.Empty) return;
            this.messenger.Write(description);
        }

        public string GetMethodDescription(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(true);
            foreach (var attribute in attributes.OfType<ActionAttribute>())
            {
                return attribute.Description;
            }
            throw new NConsolerException("Method is not marked with an Action attribute");
        }

        private IList<ParameterMetadata> GetParametersMetadata(MethodInfo method)
        {
            var result = new List<ParameterMetadata>();
            foreach (var parameter in method.GetParameters())
            {
                var parameterAttributes =
                    parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
                var parameterMetadata = new ParameterMetadata { Name = this.GetDisplayName(parameter) };
                if (parameterAttributes.Length > 0)
                {
                    var attribute = (ParameterAttribute)parameterAttributes[0];
                    parameterMetadata.Description = attribute.Description;
                    if (attribute is OptionalAttribute)
                    {
                        parameterMetadata.DefaultValue = ( (OptionalAttribute)attribute ).Default;
                    }

                }
                result.Add(parameterMetadata);

            }
            return result;
        }

        private void PrintParameterUsage(IList<ParameterMetadata> parameters)
        {
            var identation = "    ";
            var maxParameterNameLength = MaxKeyLength(parameters);
            foreach (var parameter in parameters)
            {
                if (!string.IsNullOrEmpty(parameter.Description) || parameter.DefaultValue != null)
                {
                    var difference = maxParameterNameLength - parameter.Name.Length + 2;

                    var message = identation + parameter.Name;
                    if (!string.IsNullOrEmpty(parameter.Description))
                    {
                        message += new string(' ', difference) + parameter.Description;
                    }

                    this.messenger.Write(message);
                }
                if (parameter.DefaultValue != null)
                {
                    var valueText = parameter.DefaultValue.ToString();
                    if (parameter.DefaultValue is string)
                    {
                        valueText = string.Format("'{0}'", valueText);
                    }
                    this.messenger.Write(identation + identation + "default value: " + valueText);
                }
            }
        }

        private static int MaxKeyLength(IList<ParameterMetadata> parameters) => parameters.Any() ? parameters.Select(p => p.Name).Max(k => k.Length) : 0;

        public string ProgramName()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                return this.targetType.Name.ToLower();
            }
            return new AssemblyName(entryAssembly.FullName).Name;
        }

        public bool IsSubcommandHelpRequested() => this.args.Length > 0
                   && this.args[0].ToLower() == "help"
                   && this.args.Length == 2;

        private void PrintGeneralMulticommandUsage()
        {
            this.messenger.Write(string.Format("usage: {0} <subcommand> [args]", this.ProgramName()));
            this.messenger.Write(string.Format("Type '{0} help <subcommand>' for help on a specific subcommand.", this.ProgramName()));
            this.messenger.Write(string.Empty);
            this.messenger.Write("Available subcommands:");

            foreach (var method in this.actionMethods)
            {
                this.messenger.Write(method.Name.ToLower() + " " + this.GetMethodDescription(method));
            }
        }

        private string GetDisplayName(ParameterInfo parameter)
        {
            if (this.metadata.IsRequired(parameter))
            {
                return parameter.Name;
            }
            var optional = this.metadata.GetOptional(parameter);
            var parameterName =
                ( optional.AltNames.Length > 0 ) ? optional.AltNames[0] : parameter.Name;
            if (parameter.ParameterType != typeof(bool))
            {
                parameterName += ":" + this.ValueDescription(parameter.ParameterType);
            }
            return "[/" + parameterName + "]";
        }

        public string ValueDescription(Type type)
        {
            if (type == typeof(int))
            {
                return "number";
            }
            if (type == typeof(string))
            {
                return "value";
            }
            if (type == typeof(int[]))
            {
                return "number[+number]";
            }
            if (type == typeof(string[]))
            {
                return "value[+value]";
            }
            if (type == typeof(DateTime))
            {
                return "dd-mm-yyyy";
            }
            throw new ArgumentOutOfRangeException(string.Format("Type {0} is unknown", type.Name));
        }

        #endregion
    }
}