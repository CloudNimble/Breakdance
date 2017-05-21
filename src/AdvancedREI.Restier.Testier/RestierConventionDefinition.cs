namespace AdvancedREI.Restier.Testier
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string EntitySetName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierPipelineStates? PipelineState { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierEntitySetOperations? EntitySetOperation{ get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierMethodOperations MethodOperation { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="entitySetName"></param>
        /// <param name="pipelineState"></param>
        internal RestierConventionDefinition(string name, string entitySetName, RestierPipelineStates pipelineState)
        {
            Name = name;
            EntitySetName = entitySetName;
            PipelineState = pipelineState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="entitySetName"></param>
        /// <param name="pipelineState"></param>
        /// <param name="entitySetOperation"></param>
        public RestierConventionDefinition(string name, string entitySetName, RestierPipelineStates pipelineState, RestierEntitySetOperations entitySetOperation) 
            : this(name, entitySetName, pipelineState)
        {
            EntitySetOperation = entitySetOperation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        /// <param name="methodOperation"></param>
        public RestierConventionDefinition(string name, string entitySetName, RestierPipelineStates pipelineState, RestierMethodOperations methodOperation)
            : this(name, entitySetName, pipelineState)
        {
            MethodOperation = methodOperation;
        }

        #endregion

    }

}