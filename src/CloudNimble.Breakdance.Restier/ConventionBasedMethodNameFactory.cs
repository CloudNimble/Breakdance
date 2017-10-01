using Microsoft.OData.Edm;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.Restier
{

    /// <summary>
    /// A set of string factory methods than generate Restier names for various possible operations.
    /// </summary>
    /// <remarks>
    /// This is an example of how Restier should ACTUALLY work to generate names. Some variation of thils class could be 
    /// shipping in Restier, and then utilities like Breakdance could call it, instead of re-creating functionality and
    /// having to worry about keeping it in sync.
    /// </remarks>
    public static class ConventionBasedMethodNameFactory
    {

        #region Constants

        private const string Can = "Can";

        private const string On = "On";

        private const string Ing = "ing";

        private const string Ed = "ed";

        #endregion

        #region Private Members

        /// <summary>
        /// The <see cref="RestierPipelineStates"/> to exclude from Filter name processing.
        /// </summary>
        private static List<RestierPipelineStates> ExcludedFilterStates = new List<RestierPipelineStates>
        {
            RestierPipelineStates.Authorization,
            RestierPipelineStates.PreSubmit,
            RestierPipelineStates.PostSubmit
        };

        /// <summary>
        /// The <see cref="RestierEntitySetOperations"/> to exclude from EntitySet Submit name processing.
        /// </summary>
        private static List<RestierEntitySetOperations> ExcludedEntitySetSubmitOperations = new List<RestierEntitySetOperations>
        {
            RestierEntitySetOperations.Insert,
            RestierEntitySetOperations.Update,
            RestierEntitySetOperations.Delete
        };

        /// <summary>
        /// The <see cref="RestierMethodOperations"/> to exclude from Method Submit name processing.
        /// </summary>
        private static List<RestierMethodOperations> ExcludedMethodSubmitOperations = new List<RestierMethodOperations>
        {
            RestierMethodOperations.Execute
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="entitySet">The <see cref="IEdmEntitySet"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <param name="pipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        /// <returns></returns>
        public static string GetEntitySetMethodName(IEdmEntitySet entitySet, RestierPipelineStates pipelineState, RestierEntitySetOperations operation)
        {
            if ((operation == RestierEntitySetOperations.Filter && ExcludedFilterStates.Contains(pipelineState))
                || pipelineState == RestierPipelineStates.Submit && ExcludedEntitySetSubmitOperations.Contains(operation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefix(pipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var operationName = GetOperationName(operation, pipelineState);
            var suffix = operation != RestierEntitySetOperations.Filter ? GetPipelineSuffix(pipelineState) : string.Empty;
            var entityReferenceName = GetEntityReferenceName(operation, entitySet);
            return $"{prefix}{operationName}{suffix}{entityReferenceName}";
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="operationImport">The <see cref="IEdmOperationImport"/> to generate a name for.</param>
        /// <param name="pipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="operation">The <see cref="RestierMethodOperations"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        public static string GetFunctionMethodName(IEdmOperationImport operationImport, RestierPipelineStates pipelineState, RestierMethodOperations operation)
        {
            if (pipelineState == RestierPipelineStates.Submit && ExcludedMethodSubmitOperations.Contains(operation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefix(pipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var operationName = GetOperationName(operation, pipelineState);
            var suffix = GetPipelineSuffix(pipelineState);
            var entityReferenceName = operationImport.Operation.Name;
            return $"{prefix}{operationName}{suffix}{entityReferenceName}";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates the right EntityName reference for a given Operation.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> to determine the Entity name for.</param>
        /// <param name="entitySet">The <see cref="IEdmEntitySet"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <returns>A string representing the right EntityName reference for a given Operation.</returns>
        internal static string GetEntityReferenceName(RestierEntitySetOperations operation, IEdmEntitySet entitySet)
        {
            return operation == RestierEntitySetOperations.Filter ? entitySet.Name : entitySet.EntityType().Name;
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierEntitySetOperations"/> and <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> to determine the method name for.</param>
        /// <param name="pipelineState">The <see cref="RestierPipelineStates"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        internal static string GetOperationName(RestierEntitySetOperations operation, RestierPipelineStates pipelineState)
        {
            return GetOperationName(operation.ToString(), pipelineState);
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierMethodOperations"/> and <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="operation">The <see cref="RestierMethodOperations"/> to determine the method name for.</param>
        /// <param name="pipelineState">The <see cref="RestierPipelineStates"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        internal static string GetOperationName(RestierMethodOperations operation, RestierPipelineStates pipelineState)
        {
            return GetOperationName(operation.ToString(), pipelineState);
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierMethodOperations"/> and <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="operation">The string representing the Operation to determine the method name for.</param>
        /// <param name="pipelineState">The <see cref="RestierPipelineStates"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        /// <remarks>This method is for base processing. The other overloads should be used to ensure the right name gets generated.</remarks>
        private static string GetOperationName(string operation, RestierPipelineStates pipelineState)
        {
            switch (pipelineState)
            {
                case RestierPipelineStates.PreSubmit:
                case RestierPipelineStates.PostSubmit:
                    //RWM: If the last letter of the string is an e, cut off it's head.
                    return operation.LastIndexOf("e") == operation.Length - 1 ? operation.Substring(0, operation.Length - 1) : operation;
                default:
                    return operation;
            }
        }

        /// <summary>
        /// Returns a method prefix string for a given <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="pipelineState">The <see cref="RestierPipelineStates"/> to determine the prefix for.</param>
        /// <returns></returns>
        internal static string GetPipelinePrefix(RestierPipelineStates pipelineState)
        {
            switch (pipelineState)
            {
                case RestierPipelineStates.Authorization:
                    return Can;
                case RestierPipelineStates.PreSubmit:
                case RestierPipelineStates.Submit:
                case RestierPipelineStates.PostSubmit:
                    return On;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Returns a method suffix string for a given <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="pipelineState">The <see cref="RestierPipelineStates"/> to determine the suffix for.</param>
        /// <returns></returns>
        internal static string GetPipelineSuffix(RestierPipelineStates pipelineState)
        {
            switch (pipelineState)
            {
                case RestierPipelineStates.PreSubmit:
                    return Ing;
                case RestierPipelineStates.PostSubmit:
                    return Ed;
                default:
                    return string.Empty;
            }
        }

        #endregion

    }

}