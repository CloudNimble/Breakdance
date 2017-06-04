using System;
using System.Collections.Generic;
using System.Text;

namespace AdvancedREI.Breakdance.Definitions
{
    public class TypeDefinition
    {

        public List<string> Attributes { get; set; }

        public string Class { get; set; }

        public List<MemberDefinition> Members { get; set; }


        public TypeDefinition(string classDefinition, List<string> attributes, List<MemberDefinition> members)
        {
            Class = classDefinition;
            Attributes = attributes ?? new List<string>();
            Members = members ?? new List<MemberDefinition>();
        }

    }

}