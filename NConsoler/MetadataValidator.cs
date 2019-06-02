namespace NConsoler
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Extensions;

    public class MetadataValidator
    {
        private readonly Type targetType;
        private readonly IList<MethodInfo> actionMethods;
        private readonly Metadata metadata;

        public MetadataValidator(Type targetType, IList<MethodInfo> actionMethods, Metadata metadata)
        {
            this.targetType = targetType;
            this.actionMethods = actionMethods;
            this.metadata = metadata;
        }

        public void ValidateMetadata()
        {
            this.CheckAnyActionMethodExists();
            this.IfActionMethodIsSingleCheckMethodHasParameters();

            foreach (var method in this.actionMethods)
            {
                CheckActionMethodNamesAreNotReserved(method);
                CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(method);
                this.CheckOptionalParametersAreAfterRequiredOnes(method);
                this.CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(method);
                this.CheckOptionalParametersAltNamesAreNotDuplicated(method);
            }
        }

        private static void CheckActionMethodNamesAreNotReserved(MethodBase method)
        {
            if (method.Name.ToLower() == "help")
            {
                throw new NConsolerException("Method name \"{0}\" is reserved. Please, choose another name", method.Name);
            }
        }

        private void CheckAnyActionMethodExists()
        {
            if (this.actionMethods.Count == 0)
            {
                throw new NConsolerException(
                    "Can not find any public static method marked with [Action] attribute in type \"{0}\"", this.targetType.Name);
            }
        }

        private void IfActionMethodIsSingleCheckMethodHasParameters()
        {
            if (this.actionMethods.Count == 1 && this.actionMethods[0].GetParameters().Length == 0)
            {
                throw new NConsolerException(
                    "[Action] attribute applied once to the method \"{0}\" without parameters. In this case NConsoler should not be used",
                    this.actionMethods[0].Name);
            }
        }

        private static void CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(MethodBase method)
        {
            foreach (var parameter in method.GetParameters())
            {
                var attributes = parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
                if (attributes.Length > 1)
                {
                    throw new NConsolerException("More than one attribute is applied to the parameter \"{0}\" in the method \"{1}\"",
                                                 parameter.Name, method.Name);
                }
            }
        }

        private void CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(MethodBase method)
        {
            foreach (var parameter in method.GetParameters())
            {
                if (this.metadata.IsRequired(parameter))
                {
                    continue;
                }
                var optional = this.metadata.GetOptional(parameter);
                if (optional.Default != null && optional.Default.GetType() == typeof(string) &&
                    StringToObject.CanBeConvertedToDate(optional.Default.ToString()))
                {
                    return;
                }
                if (( optional.Default == null && !parameter.ParameterType.CanBeNull() )
                    || ( optional.Default != null && !optional.Default.GetType().IsAssignableFrom(parameter.ParameterType) ))
                {
                    throw new NConsolerException(
                        "Default value for an optional parameter \"{0}\" in method \"{1}\" can not be assigned to the parameter",
                        parameter.Name, method.Name);
                }
            }
        }

        private void CheckOptionalParametersAreAfterRequiredOnes(MethodBase method)
        {
            var optionalFound = false;
            foreach (var parameter in method.GetParameters())
            {
                if (this.metadata.IsOptional(parameter))
                {
                    optionalFound = true;
                }
                else if (optionalFound)
                {
                    throw new NConsolerException(
                        "It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"",
                        method.Name, parameter.Name);
                }
            }
        }

        private void CheckOptionalParametersAltNamesAreNotDuplicated(MethodBase method)
        {
            var parameterNames = new List<string>();
            foreach (var parameter in method.GetParameters())
            {
                if (this.metadata.IsRequired(parameter))
                {
                    parameterNames.Add(parameter.Name.ToLower());
                }
                else
                {
                    if (parameterNames.Contains(parameter.Name.ToLower()))
                    {
                        throw new NConsolerException(
                            "Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters",
                            parameter.Name, method.Name);
                    }
                    parameterNames.Add(parameter.Name.ToLower());
                    var optional = this.metadata.GetOptional(parameter);
                    foreach (var altName in optional.AltNames)
                    {
                        if (parameterNames.Contains(altName.ToLower()))
                        {
                            throw new NConsolerException(
                                "Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters",
                                altName, method.Name);
                        }
                        parameterNames.Add(altName.ToLower());
                    }
                }
            }
        }
    }
}