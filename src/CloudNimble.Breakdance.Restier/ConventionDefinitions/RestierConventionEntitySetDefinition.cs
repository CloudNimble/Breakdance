namespace CloudNimble.Breakdance.Restier
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierConventionEntitySetDefinition : RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// The name of the EntitySet associated with this ConventionDefinition.
        /// </summary>
        public string EntitySetName { get; set; }

        /// <summary>
        /// The Restier Operation associated with this ConventionDefinition.
        /// </summary>
        public RestierEntitySetOperations EntitySetOperation { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="RestierConventionEntitySetDefinition"/> instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        /// <param name="entitySetName"></param>
        /// <param name="entitySetOperation"></param>
        internal RestierConventionEntitySetDefinition(string name, RestierPipelineStates pipelineState, string entitySetName, RestierEntitySetOperations entitySetOperation)
            : base(name, pipelineState)
        {
            EntitySetName = entitySetName;
            EntitySetOperation = entitySetOperation;
        }

        #endregion

    }

}