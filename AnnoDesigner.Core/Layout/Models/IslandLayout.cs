using System.Collections.Generic;
using System.Runtime.Serialization;
using AnnoDesigner.Core.Models;

namespace AnnoDesigner.Core.Layout.Models
{
    /// <summary>
    /// Container with island information and all objects.
    /// </summary>
    [DataContract]
    public class IslandLayout
    {
        [DataMember(Order = 0)]
        public string Name { get; set; }

        [DataMember(Order = 99)]
        public List<AnnoObject> Objects { get; set; }
    }
}
