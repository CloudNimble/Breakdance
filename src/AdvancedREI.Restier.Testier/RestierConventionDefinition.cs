namespace AdvancedREI.Restier.Testier
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierPipelineStates? PipelineState { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        internal RestierConventionDefinition(string name, RestierPipelineStates pipelineState)
        {
            Name = name;
            PipelineState = pipelineState;
        }

        #endregion

    }

}