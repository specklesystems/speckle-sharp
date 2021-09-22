using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.GSA.Analysis
{
    public class Case : Base
    {
        public int nativeId { get; set; }
        public string name { get; set; }

        [DetachProperty]
        public Task task { get; set; } //task reference
        public string description { get; set; } //load combination description, ex. 1.25D + 1.5L
        public Case() { }

        [SchemaInfo("GSAAnalysisCase", "Creates a Speckle structural analysis case for GSA", "GSA", "Analysis")]
        public Case(int nativeId, string name, Task task, string description) 
        {
            this.nativeId = nativeId;
            this.name = name;
            this.task = task;
            this.description = description;
        }
    }
}
