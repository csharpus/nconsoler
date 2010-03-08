using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NConsoler
{
	public class Metadata
	{
		private readonly IList<MethodInfo> _actionMethods;

		public Metadata(IList<MethodInfo> actionMethods)
		{
			_actionMethods = actionMethods;
		}

		public bool IsMulticommand
		{
			get { return _actionMethods.Count > 1; }
		}

		public bool SingleActionWithOnlyOptionalParametersSpecified()
		{
			if (IsMulticommand) return false;
			MethodInfo method = _actionMethods[0];
			return OnlyOptionalParametersSpecified(method);
		}

		private bool OnlyOptionalParametersSpecified(MethodBase method)
		{
			return method.GetParameters().All(parameter => !IsRequired(parameter));
		}

		public bool IsRequired(ParameterInfo info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
			return !info.IsOptional && (attributes.Length == 0 || attributes[0].GetType() == typeof(RequiredAttribute));
		}

		public bool IsOptional(ParameterInfo info)
		{
			return !IsRequired(info);
		}

		public Consolery.OptionalData GetOptional(ParameterInfo info)
		{
			if (info.IsOptional)
			{
				return new Consolery.OptionalData { Default = info.DefaultValue };
			}
			object[] attributes = info.GetCustomAttributes(typeof(OptionalAttribute), false);
			var attribute = (OptionalAttribute)attributes[0];
			return new Consolery.OptionalData { AltNames = attribute.AltNames, Default = attribute.Default };
		}

		public int RequiredParameterCount(MethodInfo method)
		{
			return method.GetParameters().Count(IsRequired);
		}

		public MethodInfo GetMethodByName(string name)
		{
			return _actionMethods.FirstOrDefault(method => method.Name.ToLower() == name);
		}

		public MethodInfo FirstActionMethod()
		{
			return _actionMethods.FirstOrDefault();
		}
	}
}