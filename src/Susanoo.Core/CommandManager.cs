﻿#region

using Susanoo.Pipeline.Command;
using Susanoo.Pipeline.Command.ResultSets.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Susanoo.Pipeline.Command.ResultSets.Processing.Deserialization;

#endregion

namespace Susanoo
{
    /// <summary>
    /// This class is used as the single entry point when using with Susanoo.
    /// </summary>
    public static partial class CommandManager
    {
        private static readonly IDictionary<Type, DbType> BuiltinTypeConversions =
            new ConcurrentDictionary<Type, DbType>(new Dictionary<Type, DbType>
            {
                {typeof (byte), DbType.Byte},
                {typeof (sbyte), DbType.SByte},
                {typeof (short), DbType.Int16},
                {typeof (ushort), DbType.UInt16},
                {typeof (int), DbType.Int32},
                {typeof (uint), DbType.UInt32},
                {typeof (long), DbType.Int64},
                {typeof (ulong), DbType.UInt64},
                {typeof (float), DbType.Single},
                {typeof (double), DbType.Double},
                {typeof (decimal), DbType.Decimal},
                {typeof (bool), DbType.Boolean},
                {typeof (string), DbType.String},
                {typeof (char), DbType.StringFixedLength},
                {typeof (Guid), DbType.Guid},
                {typeof (DateTime), DbType.DateTime},
                {typeof (DateTimeOffset), DbType.DateTimeOffset},
                {typeof (byte[]), DbType.Binary},
                {typeof (byte?), DbType.Byte},
                {typeof (sbyte?), DbType.SByte},
                {typeof (short?), DbType.Int16},
                {typeof (ushort?), DbType.UInt16},
                {typeof (int?), DbType.Int32},
                {typeof (uint?), DbType.UInt32},
                {typeof (long?), DbType.Int64},
                {typeof (ulong?), DbType.UInt64},
                {typeof (float?), DbType.Single},
                {typeof (double?), DbType.Double},
                {typeof (decimal?), DbType.Decimal},
                {typeof (bool?), DbType.Boolean},
                {typeof (char?), DbType.StringFixedLength},
                {typeof (Guid?), DbType.Guid},
                {typeof (DateTime?), DbType.DateTime},
                {typeof (DateTimeOffset?), DbType.DateTimeOffset}
            });

        private static readonly ConcurrentDictionary<BigInteger, ICommandProcessorWithResults> RegisteredCommandProcessors =
                    new ConcurrentDictionary<BigInteger, ICommandProcessorWithResults>();

        private static readonly ConcurrentDictionary<string, ICommandProcessorWithResults> NamedCommandProcessors =
                    new ConcurrentDictionary<string, ICommandProcessorWithResults>();

        /// <summary>
        /// Gets the expression assembly that contains runtime compiled methods used for mappings.
        /// </summary>
        /// <value>The expression assembly.</value>
        private static readonly AssemblyBuilder ExpressionAssembly = AppDomain.CurrentDomain
            .DefineDynamicAssembly(new AssemblyName("Susanoo.DynamicExpression"), AssemblyBuilderAccess.RunAndSave);

        /// <summary>
        /// The module builder for the dynamic assembly.
        /// </summary>
        private static readonly ModuleBuilder ModuleBuilder = ExpressionAssembly
            .DefineDynamicModule("Susanoo.DynamicExpression", "Susanoo.DynamicExpression.dll");

        /// <summary>
        /// Gets the dynamic namespace.
        /// </summary>
        /// <value>The dynamic namespace.</value>
        internal static ModuleBuilder DynamicNamespace
        {
            get { return ModuleBuilder; }
        }

        /// <summary>
        /// Handles exceptions in execution.
        /// </summary>
        /// <param name="commandExpressionInfo">The command expression information.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="parameters">The parameters.</param>
        public static void HandleExecutionException(
            ICommandExpressionInfo commandExpressionInfo,
            Exception ex,
            DbParameter[] parameters)
        {
            var applicable = RegisteredExceptionHandlers
                .Where(handler => handler.ConditionFunc(commandExpressionInfo, ex, parameters))
                .ToList();

            if (applicable.Any())
            {
                foreach (var handler in applicable)
                {
                    handler.Handler(commandExpressionInfo, ex, parameters);
                }
            }
            else
            {
                throw ex;
            }
        }

        /// <summary>
        /// Begins the command definition process using a Fluent API implementation, move to next step with DefineMappings on
        /// the result of this call.
        /// </summary>
        /// <typeparam name="TFilter">The type of the filter.</typeparam>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns>ICommandExpression&lt;TFilter, TResult&gt;.</returns>
        public static ICommandExpression<TFilter> DefineCommand<TFilter>(string commandText, CommandType commandType)
        {
            return CommandBuilder
                .DefineCommand<TFilter>(commandText, commandType);
        }

        /// <summary>
        /// Begins the command definition process using a Fluent API implementation, move to next step with DefineMappings on
        /// the result of this call.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns>ICommandExpression&lt;TFilter, TResult&gt;.</returns>
        public static ICommandExpression<dynamic> DefineCommand(string commandText, CommandType commandType)
        {
            return CommandBuilder
                .DefineCommand(commandText, commandType);
        }

        /// <summary>
        /// Gets the database type from the CLR type for use in parameters.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>DbType.</returns>
        public static DbType? GetDbType(Type type)
        {
            DbType dataType;
            DbType? typeToUse;
            if (!BuiltinTypeConversions.TryGetValue(type, out dataType))
                typeToUse = null;
            else
                typeToUse = dataType;

            return typeToUse;
        }

        /// <summary>
        /// Attempts to get a command processor by hash code.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="commandProcessor">The command processor.</param>
        /// <returns><c>true</c> if a command processor with the same configuration has been registered and not garbage collected,
        /// <c>false</c> otherwise.</returns>
        public static bool TryGetCommandProcessor(BigInteger hash, out ICommandProcessorWithResults commandProcessor)
        {
            var result = RegisteredCommandProcessors.TryGetValue(hash, out commandProcessor);

            return result;
        }

        /// <summary>
        /// Attempts to get a command processor by name.
        /// </summary>
        /// <param name="name">The name of the processor.</param>
        /// <param name="commandProcessor">The command processor.</param>
        /// <returns><c>true</c> if a command processor with the same configuration has been registered and not garbage collected,
        /// <c>false</c> otherwise.</returns>
        public static bool TryGetCommandProcessor(string name, out ICommandProcessorWithResults commandProcessor)
        {
            var result = NamedCommandProcessors.TryGetValue(name, out commandProcessor);

            return result;
        }

        /// <summary>
        /// Registers the command processor.
        /// </summary>
        /// <param name="processor">The processor.</param>
        /// <param name="name">The name.</param>
        public static void RegisterCommandProcessor(ICommandProcessorWithResults processor, string name)
        {
            RegisteredCommandProcessors.TryAdd(processor.CacheHash, processor);

            if (!string.IsNullOrWhiteSpace(name))
                NamedCommandProcessors.TryAdd(name, processor);
        }

        /// <summary>
        /// Flushes caches on all command processors.
        /// </summary>
        public static void FlushCacheGlobally()
        {
            foreach (var registeredCommandProcessor in RegisteredCommandProcessors)
            {
                registeredCommandProcessor.Value.FlushCache();
            }
        }

        /// <summary>
        /// Clears any column index information that may have been cached.
        /// </summary>
        /// <param name="processor">The processor.</param>
        /// <exception cref="System.ArgumentNullException">processor</exception>
        public static void ClearColumnIndexInfo(ICommandProcessorWithResults processor)
        {
            if (processor == null)
                throw new ArgumentNullException("processor");

            processor.ClearColumnIndexInfo();
        }

        /// <summary>
        /// Clears any column index information that may have been cached.
        /// </summary>
        public static void ClearColumnIndexInfo()
        {
            RegisteredCommandProcessors
                .Select(kvp => kvp.Value)
                .ToList()
                .ForEach(processor => processor.ClearColumnIndexInfo());
        }

        /// <summary>
        /// Flushes caches on a specific named command processor.
        /// </summary>
        /// <param name="name">The name of the command processor.</param>
        public static void FlushCache(string name)
        {
            ICommandProcessorWithResults reference;
            if (NamedCommandProcessors.TryGetValue(name, out reference))
                reference.FlushCache();
        }

        /// <summary>
        /// Saves the dynamic assembly to disk.
        /// </summary>
        /// <param name="assemblyFileName">Name of the assembly file.</param>
        public static void SaveDynamicAssemblyToDisk(string assemblyFileName)
        {
            ExpressionAssembly.Save(assemblyFileName);
        }
    }

    //IoC Methods
    public static partial class CommandManager
    {
        private static readonly IList<ExceptionHandler> RegisteredExceptionHandlers = new List<ExceptionHandler>();

        private static ICommandExpressionBuilder _commandBuilder = new CommandBuilder();

        private  static IDeserializerResolver _deserializerResolver = new DeserializerResolver();

        private static Func<string, IDatabaseManager> _databaseManagerFactoryMethod = connectionStringName =>
                    new DatabaseManager(connectionStringName);

        /// <summary>
        /// Gets the command builder.
        /// </summary>
        /// <value>The command builder.</value>
        public static ICommandExpressionBuilder CommandBuilder
        {
            get { return _commandBuilder; }
        }

        /// <summary>
        /// Gets the deserializer resolver.
        /// </summary>
        /// <value>The deserializer resolver.</value>
        public static IDeserializerResolver DeserializerResolver
        {
            get { return _deserializerResolver; }
        }

        /// <summary>
        /// Registers an exception handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public static void RegisterExecutionExceptionHandler(ExceptionHandler handler)
        {
            RegisteredExceptionHandlers.Add(handler);
        }

        /// <summary>
        /// Registers the database manager.
        /// </summary>
        /// <param name="databaseManagerFactoryMethod">The database manager factory method.</param>
        [Obsolete("Build your own instance.", false)]
        public static void RegisterDatabaseManagerFactory(Func<string, IDatabaseManager> databaseManagerFactoryMethod)
        {
            if (databaseManagerFactoryMethod != null)
                _databaseManagerFactoryMethod = databaseManagerFactoryMethod;
        }

        /// <summary>
        /// Registers a command builder for the CommandManager to use when building commands.
        /// This is an ideal extension point for providing default command options.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static void RegisterCommandBuilder(ICommandExpressionBuilder builder)
        {
            _commandBuilder = builder;
        }

        /// <summary>
        /// Registers a DeserializerResolver for the CommandManager to use when building commands.
        /// This is an ideal extension point for extending or modifying deserialization rules.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        public static void RegisterDeserializerResolver(IDeserializerResolver resolver)
        {
            _deserializerResolver = resolver;
        }

        /// <summary>
        /// Gets the database manager.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>IDatabaseManager.</returns>
        /// <value>The database manager.</value>
        [Obsolete("Build your own instance.", false)]
        public static IDatabaseManager BuildDatabaseManager(string connectionString)
        {
            return _databaseManagerFactoryMethod(connectionString);
        }
    }
}