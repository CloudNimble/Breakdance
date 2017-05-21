using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Providers.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Extensions;

namespace AdvancedREI.Restier.Testier
{

    /// <summary>
    /// 
    /// </summary>
    public static class TestierGenerator
    {

        #region Public Methods

        /// <summary>
        /// Generates 
        /// </summary>
        /// <param name="edmModel"></param>
        /// <returns></returns>
        public static List<RestierConventionDefinition> GenerateConventionDefinitions(this IEdmModel edmModel)
        {
            var entries = new List<RestierConventionDefinition>();
            var model = (EdmModel)edmModel;

            //RWM: Cycle through the EntitySets first.
            foreach (var entitySet in model.EntityContainer.EntitySets().OrderBy(c => c.Name))
            {
                foreach (var pipelineState in Enum.GetValues(typeof(RestierPipelineStates)).Cast<RestierPipelineStates>())
                {
                    foreach (var operation in Enum.GetValues(typeof(RestierEntitySetOperations)).Cast<RestierEntitySetOperations>())
                    {
                        var functionName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(entitySet, pipelineState, operation);
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            entries.Add(new RestierConventionEntitySetDefinition(functionName, pipelineState, entitySet.Name, operation));
                        }
                    }
                }
                //TODO: Handle EntitySet-bound functions.
            }

            foreach (var function in model.EntityContainer.OperationImports())
            {
                foreach (var pipelineState in Enum.GetValues(typeof(RestierPipelineStates)).Cast<RestierPipelineStates>())
                {
                    foreach (var operation in Enum.GetValues(typeof(RestierMethodOperations)).Cast<RestierMethodOperations>())
                    {
                        var functionName = ConventionBasedMethodNameFactory.GetFunctionMethodName(function, pipelineState, operation);
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            entries.Add(new RestierConventionMethodDefinition(functionName, pipelineState, function.Name, operation));
                        }
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Generates 
        /// </summary>
        /// <param name="edmModel"></param>
        /// <param name="addTableSeparators"></param>
        /// <returns></returns>
        public static string GenerateConventionReport(this IEdmModel edmModel, bool addTableSeparators = false)
        {
            var sb = new StringBuilder();
            var conventions = GenerateConventionDefinitions(edmModel);

            foreach (var entitySet in conventions.OfType<RestierConventionEntitySetDefinition>().GroupBy(c => c.EntitySetName).OrderBy(c => c.Key))
            {
                if (addTableSeparators)
                {
                    sb.AppendLine($"-- {entitySet.Key} --");
                }

                foreach (var definition in entitySet.OrderBy(c => c.PipelineState).ThenBy(c => c.EntitySetOperation))
                {
                    sb.AppendLine(definition.Name);
                }

                if (addTableSeparators)
                {
                    sb.AppendLine();
                }
            }

            foreach (var function in conventions.OfType<RestierConventionMethodDefinition>().GroupBy(c => c.MethodName).OrderBy(c => c.Key))
            {
                if (addTableSeparators)
                {
                    sb.AppendLine($"-- OperationImports --");
                }

                foreach (var definition in function.OrderBy(c => c.PipelineState).ThenBy(c => c.MethodOperation))
                {
                    sb.AppendLine(definition.Name);
                }

                if (addTableSeparators)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// An extension method that generates a Markdown table of all of the possible Restier methods for the given API in the first column, and a boolean
        /// indicating whether or not the method was found in the second column.
        /// </summary>
        /// <param name="api">The <see cref="ApiBase"/> instance to process, typically inheriting from <see cref="EntityFrameworkApi{T}"/>.</param>
        /// <returns>A string containing the Markdown table of results.</returns>
        public static async Task<string> GenerateVisibilityMatrix(this ApiBase api)
        {
            var sb = new StringBuilder();
            var model = (EdmModel) await api.GetModelAsync(default(CancellationToken));
            var apiType = api.GetType();
            
            var conventions = model.GenerateConventionDefinitions();
            var entitySetMatrix = conventions.OfType<RestierConventionEntitySetDefinition>().ToDictionary(c => c, c => false);
            var methodMatrix = conventions.OfType<RestierConventionMethodDefinition>().ToDictionary(c => c, c => false);

            foreach (var definition in entitySetMatrix.ToList())
            {
                var value = false;
                switch (definition.Key.PipelineState)
                {
                    case RestierPipelineStates.Authorization:
                        value = IsAuthorizerMethodAccessible(apiType, definition.Key.Name);
                        break;
                    default:
                        if (definition.Key.EntitySetOperation == RestierEntitySetOperations.Filter)
                        {
                            value = IsFilterMethodAccessible(apiType, definition.Key.Name);
                        }
                        else
                        {
                            value = IsInterceptorMethodAccessible(apiType, definition.Key.Name);
                        }
                        break;
                }
                entitySetMatrix[definition.Key] = value;
            }

            foreach (var definition in methodMatrix.ToList())
            {
                var value = false;
                switch (definition.Key.PipelineState)
                {
                    case RestierPipelineStates.Authorization:
                        value = IsAuthorizerMethodAccessible(apiType, definition.Key.Name);
                        break;
                    default:
                        value = IsInterceptorMethodAccessible(apiType, definition.Key.Name);
                        break;
                }
                methodMatrix[definition.Key] = value;
            }

            sb.AppendLine($"--------------------------------------------------");
            sb.AppendLine(string.Format("{0,-40} | {1,7}", "Function Name", "Found?"));
            sb.AppendLine($"--------------------------------------------------");
            foreach (var result in entitySetMatrix)
            {
                sb.AppendLine(string.Format("{0,-40} | {1,7}", result.Key.Name, result.Value));
            }
            foreach (var result in methodMatrix)
            {
                sb.AppendLine(string.Format("{0,-40} | {1,7}", result.Key.Name, result.Value));
            }
            sb.AppendLine($"--------------------------------------------------");

            return sb.ToString();
        }

        /// <summary>
        /// An extension method that generates the Visibility Matrix for the current Api and writes it to a text file.
        /// </summary>
        /// <param name="api">The <see cref="ApiBase"/> instance to build the Visibility Matrix for.</param>
        /// <param name="suffix">A string to append to the Api name when writing the text file.</param>
        public static async Task WriteCurrentVisibilityMatrix(this ApiBase api, string suffix = "ApiSurface")
        {
            var report = await api.GenerateVisibilityMatrix();
            System.IO.File.WriteAllText($"{api.GetType().Name}-{suffix}.txt", report);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static bool IsAuthorizerMethodAccessible(Type api, string methodName)
        {
            //ConventionBasedChangeSetItemAuthorizer.cs:46
            Type returnType = typeof(bool);
            var method = api.GetQualifiedMethod(methodName);

            if (method != null && (method.IsFamily || method.IsFamilyOrAssembly) &&
                method.ReturnType == returnType)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static bool IsInterceptorMethodAccessible(Type api, string methodName)
        {
            //ConventionBasedChangeSetItemFilter.cs:116
            var method = api.GetQualifiedMethod(methodName);

            if (method != null &&
                (method.ReturnType == typeof(void) ||
                typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static bool IsFilterMethodAccessible(Type api, string methodName)
        {
            //ConventionBasedQueryExpressionProcessor.cs:110
            var method = api.GetQualifiedMethod(methodName);

            if (method != null && (method.IsFamily || method.IsFamilyOrAssembly))
            {
                var parameter = method.GetParameters().SingleOrDefault();
                if (parameter != null &&
                    parameter.ParameterType == method.ReturnType)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

    }

}