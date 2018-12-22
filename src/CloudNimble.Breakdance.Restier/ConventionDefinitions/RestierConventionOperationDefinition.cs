using Microsoft.Restier.Core;

namespace CloudNimble.Breakdance.Restier
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierConventionMethodDefinition : RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierOperationMethods MethodOperation { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        /// <param name="methodName"></param>
        /// <param name="methodOperation"></param>
        public RestierConventionMethodDefinition(string name, RestierPipelineStates pipelineState, string methodName, RestierOperationMethods methodOperation)
            : base(name, pipelineState)
        {
            MethodName = methodName;
            MethodOperation = methodOperation;
        }

        #endregion

    }

}