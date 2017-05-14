using System;
using System.Collections.Generic;

namespace AdvancedREI.Restier.Tests.Testier.Model
{

    /// <summary>
    /// 
    /// </summary>
    public class Team
    {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public virtual ICollection<Player> Players { get; set; }

    }

}