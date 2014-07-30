﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Susanoo
{
    /// <summary>
    /// Allows configuration of the Susanoo mapper at the property level during command definition.
    /// </summary>
    public interface IPropertyMappingConfiguration : IFluentPipelineFragment
    {
        /// <summary>
        /// Uses the specified alias when mapping from the data call.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns>Susanoo.IResultMappingExpression&lt;TFilter,TResult&gt;.</returns>
        IPropertyMappingConfiguration UseAlias(string alias);

        /// <summary>
        /// Processes the value in some form before assignment.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <returns>IPropertyMappingConfiguration&lt;TRecord&gt;.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        IPropertyMappingConfiguration ProcessValueUsing(Expression<Func<Type, object, object>> process);

        /// <summary>
        /// Gets the property metadata.
        /// </summary>
        /// <value>The property metadata.</value>
        PropertyInfo PropertyMetadata { get; }
    }

    /// <summary>
    /// Allows retrieval of configurations at the property level.
    /// </summary>
    public interface IPropertyMapping : IFluentPipelineFragment
    {
        /// <summary>
        /// Assembles the mapping expression.
        /// </summary>
        /// <param name="propertyExpression">The property.</param>
        /// <returns>Expression&lt;Action&lt;IDataRecord&gt;&gt;.</returns>
        Expression<Action<IDataRecord>> AssembleMappingExpression(MemberExpression propertyExpression);

        /// <summary>
        /// Gets the property metadata.
        /// </summary>
        /// <value>The property metadata.</value>
        PropertyInfo PropertyMetadata { get; }

        /// <summary>
        /// Gets or sets the name of the return column.
        /// </summary>
        /// <value>The name of the return.</value>
        string ActiveAlias { get; }
    }
}