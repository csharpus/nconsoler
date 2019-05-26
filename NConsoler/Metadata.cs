namespace NConsoler
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class Metadata
    {
        private readonly IList<MethodInfo> actionMethods;

        public Metadata(IList<MethodInfo> actionMethods) => this.actionMethods = actionMethods;

        public bool IsMulticommand => this.actionMethods.Count > 1;

        public bool SingleActionWithOnlyOptionalParametersSpecified()
        {
            if (this.IsMulticommand) return false;
            var method = this.actionMethods[0];
            return this.OnlyOptionalParametersSpecified(method);
        }

        private bool OnlyOptionalParametersSpecified(MethodBase method) => method.GetParameters().All(parameter => !this.IsRequired(parameter));

        public bool IsRequired(ParameterInfo info)
        {
            var attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
            return !info.IsOptional && ( attributes.Length == 0 || attributes[0].GetType() == typeof(RequiredAttribute) );
        }

        public bool IsOptional(ParameterInfo info) => !this.IsRequired(info);

        public Consolery.OptionalData GetOptional(ParameterInfo info)
        {
            if (info.IsOptional)
            {
                return new Consolery.OptionalData { Default = info.DefaultValue };
            }
            var attributes = info.GetCustomAttributes(typeof(OptionalAttribute), false);
            var attribute = (OptionalAttribute)attributes[0];
            return new Consolery.OptionalData { AltNames = attribute.AltNames, Default = attribute.Default };
        }

        public int RequiredParameterCount(MethodInfo method) => method.GetParameters().Count(this.IsRequired);

        public MethodInfo GetMethodByName(string name) => this.actionMethods.FirstOrDefault(method => method.Name.ToLower() == name);

        public MethodInfo FirstActionMethod() => this.actionMethods.FirstOrDefault();
    }
}