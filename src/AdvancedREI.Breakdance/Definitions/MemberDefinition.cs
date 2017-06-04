using System;
using System.Collections.Generic;
using System.Text;

namespace AdvancedREI.Breakdance.Definitions
{
    public class MemberDefinition
    {

        public List<string> Attributes { get; set; }

        public string Member { get; set; }


        public MemberDefinition(string member, List<string> attributes)
        {
            Member = member;
            Attributes = attributes ?? new List<string>();
        }

    }
}
