using Microsoft.OData.Edm;
using System.Collections.Generic;

namespace AdvancedREI.Restier.Testier
{

    /// <summary>
    /// 
    /// </summary>
    public static class ConventionBasedMethodNameFactory
    {

        #region Constants

        private const string Can = "Can";

        private const string On = "On";

        private const string Ing = "ing";

        private const string Ed = "ed";

        #endregion

        #region Private Members

        public static List<RestierPipelineStates> ExcludedFilterStates = new List<RestierPipelineStates>
        {
            RestierPipelineStates.PreSubmit,
            RestierPipelineStates.PostSubmit
        };

        public static List<RestierEntitySetOperations> ExcludedEntitySetSubmitOperations = new List<RestierEntitySetOperations>
        {
            RestierEntitySetOperations.Insert,
            RestierEntitySetOperations.Update,
            RestierEntitySetOperations.Delete
        };

        public static List<RestierMethodOperations> ExcludedMethodSubmitOperations = new List<RestierMethodOperations>
        {
            RestierMethodOperations.Execute
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entitySet"></param>
        /// <param name="pipelineState"></param>
        /// <param name="operation"></param>
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
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="entitySet"></param>
        /// <returns></returns>
        internal static string GetEntityReferenceName(RestierEntitySetOperations operation, IEdmEntitySet entitySet)
        {
            return operation == RestierEntitySetOperations.Filter ? entitySet.EntityType().Name : entitySet.Name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="pipelineState"></param>
        /// <returns></returns>
        private static string GetOperationName(RestierEntitySetOperations operation, RestierPipelineStates pipelineState)
        {
            return GetOperationName(operation.ToString(), pipelineState);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="pipelineState"></param>
        /// <returns></returns>
        private static string GetOperationName(RestierMethodOperations operation, RestierPipelineStates pipelineState)
        {
            return GetOperationName(operation.ToString(), pipelineState);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="pipelineState"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="pipelineState"></param>
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
        /// 
        /// </summary>
        /// <param name="pipelineState"></param>
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