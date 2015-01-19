﻿#region

using System;
using System.Collections.Generic;

#endregion

namespace Susanoo
{
    /// <summary>
    /// Provides a common form of storage and retrieval for mapping details of results.
    /// </summary>
    /// <typeparam name="TFilter">The type of the filter.</typeparam>
    public interface ICommandResultImplementor<TFilter> : IFluentPipelineFragment
    {
        /// <summary>
        /// Retrieves a mapping.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <returns>IResultMappingExpression&lt;TFilter, TResult&gt;.</returns>
        IResultMappingExpression<TFilter, TResult> RetrieveMapping<TResult>()
            where TResult : new();

        /// <summary>
        /// Retrieves a mapping exporter.
        /// </summary>
        /// <param name="resultType">Type of the result.</param>
        /// <returns>IResultMappingExpression&lt;TFilter, TResult&gt;.</returns>
        IResultMappingExport RetrieveExporter(Type resultType);

        /// <summary>
        /// Stores a mapping.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="mapping">The mapping.</param>
        void StoreMapping<TResult>(Action<IResultMappingExpression<TFilter, TResult>> mapping)
            where TResult : new();

        /// <summary>
        /// Exports a results mappings for processing.
        /// </summary>
        /// <typeparam name="TResultType">The type of the result type.</typeparam>
        /// <returns>IDictionary&lt;System.String, IPropertyMapping&gt;.</returns>
        IDictionary<string, IPropertyMapping> Export<TResultType>()
            where TResultType : new();

        /// <summary>
        /// Exports a results mappings for processing.
        /// </summary>
        /// <param name="resultType">Type of the result.</param>
        /// <returns>IDictionary&lt;System.String, IPropertyMapping&gt;.</returns>
        IDictionary<string, IPropertyMapping> Export(Type resultType);
    }
}