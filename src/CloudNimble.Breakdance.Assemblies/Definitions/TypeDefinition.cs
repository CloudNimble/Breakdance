using System.Collections.Generic;
using System.Diagnostics;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// 
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TypeDefinition
    {

        #region Properties

        /// <summary>
        /// A <see cref="List{String}"/> containging the full name of each attribute on the type.
        /// </summary>
        public List<string> Attributes { get; private set; }

        /// <summary>
        /// The full name of the type member in question.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<MemberDefinition> Members { get; private set; }

        /// <summary>
        /// Returns a string suitable for display in the debugger. Ensures such strings are compiled by the runtime and not interpreted by the currently-executing language.
        /// </summary>
        /// <remarks>http://blogs.msdn.com/b/jaredpar/archive/2011/03/18/debuggerdisplay-attribute-best-practices.aspx</remarks>
        internal string DebuggerDisplay => $"TypeName: {TypeName}, AttributeCount: {Attributes.Count}, MemberCount: {Members.Count}";

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