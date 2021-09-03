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

        [SchemaInfo("AnalysisCase", "Creates a Speckle structural analysis case for GSA", "GSA", "Analysis")]
        public Case(int nativeId, string name, Task task, string description) 
        {
            this.nativeId = nativeId;
            this.name = name;
            this.task = task;
            this.description = description;
        }
    }

    public class Task : Base
    {
        public int nativeId { get; set; } //equiv to num
        public string name { get; set; }
        public string stage { get; set; }
        public string solver { get; set; }
        public SolutionType solutionType { get; set; }
        public int modeParameter1 { get; set; } //start mode
        public int modeParameter2 { get; set; } //number of modes
        public int numIterations { get; set; }
        public string PDeltaOption { get; set; }
        public string PDeltaCase { get; set; }
        public string PrestressCase { get; set; }
        public string resultSyntax { get; set; }
        public PruningOption prune { get; set; }
        public Task() { }

        [SchemaInfo("AnalysisTask", "Creates a Speckle structural analysis task for GSA", "GSA", "Analysis")]
        public Task(int nativeId, string name)
        {
            this.nativeId = nativeId;
            this.name = name;
        }
    }

    public enum SolutionType
    {
        Undefined,   //no solution specified
        Static,
        Modal,
        Ritz,
        Buckling,
        StaticPDelta,
        ModalPDelta,
        RitzPDelta,
        Mass,
        Stability,
        StabilityPDelta,
        BucklingNonLinear,
        Influence
    }

    public enum PruningOption
    {        
        None,
        Influence
    }
}
