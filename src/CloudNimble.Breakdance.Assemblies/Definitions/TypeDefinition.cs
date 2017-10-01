using System.Collections.Generic;

namespace CloudNimble.Breakdance.Assemblies.Definitions
{

    /// <summary>
    /// 
    /// </summary>
    public class TypeDefinition
    {

        #region Properties

        /// <summary>
        /// A <see cref="List{String}"/> containging the full name of each attribute on the type.
        /// </summary>
        public List<string> Attributes { get; set; }

        /// <summary>
        /// The full name of the type member in question.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<MemberDefinition> Members { get; set; }

        #endregion

        #region Construction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="classDefinition"></param>
        /// <param name="attributes"></param>
        /// <param name="members"></param>
        public TypeDefinition(string classDefinition, List<string> attributes, List<MemberDefinition> members)
        {
            TypeName = classDefinition;
            Attributes = attributes ?? new List<string>();
            Members = members ?? new List<MemberDefinition>();
        }

        #endregion

    }

}