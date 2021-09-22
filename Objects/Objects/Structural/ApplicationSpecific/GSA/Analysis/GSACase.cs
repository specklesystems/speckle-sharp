using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.GSA.Analysis
{
    public class GSACase : Base
    {
        public int nativeId { get; set; }
        public string name { get; set; }

        [DetachProperty]
        public GSATask task { get; set; } //task reference
        public string description { get; set; } //load combination description, ex. 1.25D + 1.5L
        public GSACase() { }

        [SchemaInfo("GSAAnalysisCase", "Creates a Speckle structural analysis case for GSA", "GSA", "Analysis")]
        public GSACase(int nativeId, string name, GSATask task, string description) 
        {
            this.nativeId = nativeId;
            this.name = name;
            this.task = task;
            this.description = description;
        }
    }
}
