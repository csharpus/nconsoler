namespace NConsoler
{
    using System;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using System.Diagnostics;

    /// <summary>
    /// Entry point for NConsoler applications
    /// </summary>
    public sealed class Consolery
    {
        /// <summary>
        /// Runs an appropriate Action method.
        /// Uses the class this call lives in as target type and command line arguments from Environment
        /// </summary>
        public static void Run()
        {
            var declaringType = new StackTrace().GetFrame(1).GetMethod().DeclaringType;
            var args = new string[Environment.GetCommandLineArgs().Length - 1];
            new List<string>(Environment.GetCommandLineArgs()).CopyTo(1, args, 0, Environment.GetCommandLineArgs().Length - 1);
            Run(declaringType, args);
        }

        /// <summary>
        /// Runs an appropriate Action method
        /// </summary>
        /// <param name="targetType">Type where to search for Action methods</param>
        /// <param name="args">Arguments that will be converted to Action method arguments</param>
        public static void Run(Type targetType, string[] args) => Run(targetType, args, new ConsoleMessenger());

        /// <summary>
        /// Runs an appropriate Action method
        /// </summary>
        /// <param name="target">instance where to search for Action methods</param>
        /// <param name="args">Arguments that will be converted to Action method arguments</param>
        public static void Run(object target, string[] args) => Run(target, args, new ConsoleMessenger());

        /// <summary>
        /// Runs an appropriate Action method
        /// </summary>
        /// <param name="targetType">Type where to search for Action methods</param>
        /// <param name="args">Arguments that will be converted to Action method arguments</param>
        /// <param name="messenger">Uses for writing messages instead of Console class methods</param>
        /// <param name="notationType">Switch for command line syntax. Windows: /param:value Linux: -param value</param>
        public static void Run(Type targetType, string[] args, IMessenger messenger, Notation notationType = Notation.Windows)
        {
            try
            {
                new Consolery(targetType, null, args, messenger, notationType).RunAction();
            }
            catch (NConsolerException e)
            {
                messenger.Write(e.Message);
                const int genericErrorExitCode = 1;
                Environment.ExitCode = genericErrorExitCode;
            }
        }

        /// <summary>
        /// Runs an appropriate Action method
        /// </summary>
        /// <param name="target">Type where to search for Action methods</param>
        /// <param name="args">Arguments that will be converted to Action method arguments</param>
        /// <param name="messenger">Uses for writing messages instead of Console class methods</param>
        /// <param name="notationType">Switch for command line syntax. Windows: /param:value Linux: -param value</param>
        public static void Run(object target, string[] args, IMessenger messenger, Notation notationType = Notation.Windows)
        {
            Contract.Requires(target != null);
            try
            {
                new Consolery(target.GetType(), target, args, messenger, notationType).RunAction();
            }
            catch (NConsolerException e)
            {
                messenger.Write(e.Message);
            }
        }

        /// <summary>
        /// Validates specified type and throws NConsolerException if an error
        /// </summary>
        /// <param name="targetType">Type where to search for Action methods</param>
        public static void Validate(Type targetType) => new Consolery(targetType, null, new string[] { }, new ConsoleMessenger(), Notation.None).ValidateMetadata();

        private readonly object target;
        private readonly Type targetType;
        private readonly string[] args;
        private readonly List<MethodInfo> actionMethods = new List<MethodInfo>();
        private readonly IMessenger messenger;
        private readonly INotationStrategy notation;
        private readonly Metadata metadata;
        private readonly MetadataValidator metadataValidator;

        public Consolery(Type targetType, object target, string[] args, IMessenger messenger, Notation notationType)
        {
            Contract.Requires(targetType != null);
            Contract.Requires(args != null);
            Contract.Requires(messenger != null);

            this.target = target;
            this.targetType = targetType;
            this.args = args;
            this.messenger = messenger;

            this.actionMethods = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttributes(false).OfType<ActionAttribute>().Any())
                .ToList();

            this.metadata = new Metadata(this.actionMethods);
            this.metadataValidator = new MetadataValidator(this.targetType, this.actionMethods, this.metadata);
            if (notationType == Notation.Windows)
            {
                this.notation = new WindowsNotationStrategy(this.args, this.messenger, this.metadata, this.targetType, this.actionMethods);
            }
            else
            {
                this.notation = new LinuxNotationStrategy(this.args, this.messenger, this.metadata);
            }
        }

        private void RunAction()
        {
            this.ValidateMetadata();
            if (this.IsHelpRequested())
            {
                this.notation.PrintUsage();
                return;
            }

            var currentMethod = this.notation.GetCurrentMethod();
            if (currentMethod == null)
            {
                this.notation.PrintUsage();
                throw new NConsolerException("Unknown subcommand \"{0}\"", this.args[0]);
            }
            this.notation.ValidateInput(currentMethod);
            this.InvokeMethod(currentMethod);
        }

        private void ValidateMetadata() => this.metadataValidator.ValidateMetadata();

        public struct ParameterData
        {
            public readonly int Position;
            public readonly Type Type;

            public ParameterData(int position, Type type)
            {
                this.Position = position;
                this.Type = type;
            }
        }

        public class OptionalData
        {
            public OptionalData() => this.AltNames = new string[] { };

            public string[] AltNames { get; set; }

            public object Default { get; set; }
        }

        private bool IsHelpRequested() => ( this.args.Length == 0 && !this.metadata.SingleActionWithOnlyOptionalParametersSpecified() )
                   || ( this.args.Length > 0 && ( this.args[0] == "/?"
                                            || this.args[0] == "/help"
                                            || this.args[0] == "/h"
                                            || this.args[0] == "help" ) );

        private delegate void Runner(object target, object[] parameters);

        private void InvokeMethod(MethodInfo method)
        {
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");
            var parameters = GetParameters(method, parametersParameter);

            var targetParameter = Expression.Parameter(typeof(object), "target");

            var instanceExpression = GetInstanceExpression(method, targetParameter);
            var methodCall = Expression.Call(instanceExpression, method, parameters);

            // ((Program) target).DoWork((string) parameters[0], (int) parameters[1]);
            Expression
                .Lambda<Runner>(methodCall, targetParameter, parametersParameter)
                .Compile()
                .Invoke(this.target, this.notation.BuildParameterArray(method));
        }

        private static UnaryExpression GetInstanceExpression(MethodInfo method, ParameterExpression targetParameter) => !method.IsStatic
                       ? Expression.Convert(targetParameter, method.ReflectedType)
                       : null;

        private static IEnumerable<Expression> GetParameters(MethodInfo method, ParameterExpression parametersParameter)
        {
            var parameters = new List<Expression>();
            var paramInfos = method.GetParameters();
            for (var i = 0; i < paramInfos.Length; i++)
            {
                var arrayValue = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var convertExpression = Expression.Convert(arrayValue, paramInfos[i].ParameterType);

                parameters.Add(convertExpression);
            }
            return parameters;
        }
    }

    public enum Notation
    {
        None = 0,
        Windows = 1,
        Linux = 2
    }

    public class ParameterMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
    }
}