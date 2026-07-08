using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AnnoDesigner.Core.Layout.Models
{
    /// <summary>
    /// Container for a game session (e.g. "Old World", "New World") with its islands.
    /// </summary>
    [DataContract]
    public class SessionLayout
    {
        [DataMember(Order = 0)]
        public string Name { get; set; }

        [DataMember(Order = 99)]
        public List<IslandLayout> Islands { get; set; }
    }
}
