using System;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.Tests.Restier.Model
{

    /// <summary>
    /// 
    /// </summary>
    public class Sport
    {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string DateStarted { get; set; }

        public virtual ICollection<Team> Teams { get; set; }

    }

}