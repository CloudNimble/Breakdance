﻿using System.Collections.Generic;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// Allows for the storage of metadata information for a specific type member.
    /// </summary>
    public class MemberDefinition
    {

        #region Properties

        /// <summary>
        /// A <see cref="List{String}"/> containing the full name of each attribute on the type member.
        /// </summary>
        public List<string> Attributes { get; private set; }

        /// <summary>
        /// The full name of the type member in question.
        /// </summary>
        public string MemberName { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="attributes"></param>
        public MemberDefinition(string member, List<string> attributes)
        {
            MemberName = member;
            Attributes = attributes ?? new List<string>();
        }

        #endregion

    }
}
