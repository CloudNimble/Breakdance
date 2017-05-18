using Microsoft.OData.Edm;
using Microsoft.Restier.Providers.EntityFramework;
using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedREI.Restier.Testier
{

    /// <summary>
    /// 
    /// </summary>
    public static class TestierGenerator
    {

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edmModel"></param>
        /// <param name="addTableSeparators"></param>
        /// <returns></returns>
        public static string GenerateConventionList(this IEdmModel edmModel, bool addTableSeparators = false)
        {
            var sb = new StringBuilder();
            var model = (EdmModel)edmModel;

            //RWM: Cycle through the EntitySets first.
            foreach (var entitySet in model.EntityContainer.EntitySets().OrderBy(c => c.Name))
            {
                if (addTableSeparators)
                {
                    sb.AppendLine($"-- {entitySet.Name} --");
                }
                foreach (var pipelineState in Enum.GetValues(typeof(RestierPipelineStates)).Cast<RestierPipelineStates>())
                {
                    foreach (var operation in Enum.GetValues(typeof(RestierEntitySetOperations)).Cast<RestierEntitySetOperations>())
                    {
                        var functionName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(entitySet, pipelineState, operation);
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            sb.Append(functionName + Environment.NewLine);
                        }
                    }
                }

                //TODO: Handle EntitySet-bound functions.
                if (addTableSeparators)
                {
                    sb.AppendLine();
                }
            }

            foreach (var function in model.EntityContainer.OperationImports())
            {
                if (addTableSeparators)
                {
                    sb.AppendLine($"-- OperationImports --");
                }
                foreach (var pipelineState in Enum.GetValues(typeof(RestierPipelineStates)).Cast<RestierPipelineStates>())
                {
                    foreach (var operation in Enum.GetValues(typeof(RestierMethodOperations)).Cast<RestierMethodOperations>())
                    {
                        var functionName = ConventionBasedMethodNameFactory.GetFunctionMethodName(function, pipelineState, operation);
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            sb.Append(functionName + Environment.NewLine);
                        }
                    }
                }
                if (addTableSeparators)
                {
                    sb.AppendLine();
                }

                //function.Operation.Name
            }

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="edmModel"></param>
        /// <returns></returns>
        public static string GenerateVisibilityMatrix<T>(this EntityFrameworkApi<T> api, IEdmModel edmModel) where T : DbContext
        {
            var sb = new StringBuilder();
            var model = (EdmModel)edmModel;
            var apiType = api.GetType();

            var conventions = edmModel.GenerateConventionList();
            var matrix = conventions.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(c => c, c => false);

            var authorizerMethods = matrix.Where(c => c.Key.StartsWith("Can")).Select(c => c.Key).ToList();
            foreach (var method in authorizerMethods)
            {
                matrix[method] = IsAuthorizerMethodAccessible(apiType, method);
            }

            var interceptorMethods = matrix.Where(c => c.Key.StartsWith("On") && !c.Key.Contains("Filter")).Select(c => c.Key).ToList();
            foreach (var method in interceptorMethods)
            {
                matrix[method] = IsInterceptorMethodAccessible(apiType, method);
            }

            var filterMethods = matrix.Where(c => c.Key.StartsWith("On") && c.Key.Contains("Filter")).Select(c => c.Key).ToList();
            foreach (var method in filterMethods)
            {
                matrix[method] = IsFilterMethodAccessible(apiType, method);
            }

            sb.AppendLine($"--------------------------------------------------");
            sb.AppendLine(string.Format("{0,-40} | {1,7}", "Function Name", "Found"));
            sb.AppendLine($"--------------------------------------------------");
            foreach (var result in matrix)
            {
                sb.AppendLine(string.Format("{0,-40} | {1,7}", result.Key, result.Value));
            }
            sb.AppendLine($"--------------------------------------------------");

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="edmModel"></param>
        public static void WriteCurrentVisibilityReport<T>(this EntityFrameworkApi<T> api, IEdmModel edmModel) where T : DbContext
        {
            var report = api.GenerateVisibilityMatrix(edmModel);
            System.IO.File.WriteAllText($"{api.GetType().Name}-ApiSurface.txt", report);
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