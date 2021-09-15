using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Analysis;
using Objects.Structural.Loading;

namespace Objects.Structural.Results
{
    public class ResultGlobal : Result
    {
        [DetachProperty]
        public Model model { get; set; }
        public double loadX { get; set; }
        public double loadY { get; set; }
        public double loadZ { get; set; }
        public double loadXX { get; set; }
        public double loadYY { get; set; }
        public double loadZZ { get; set; }
        public double reactionX { get; set; }
        public double reactionY { get; set; }
        public double reactionZ { get; set; }
        public double reactionXX { get; set; }
        public double reactionYY { get; set; }
        public double reactionZZ { get; set; }
        public double mode { get; set; }
        public double frequency { get; set; }
        public double loadFactor { get; set; }
        public double modalStiffness { get; set; }
        public double modalGeoStiffness { get; set; }
        public double effMassX { get; set; }
        public double effMassY { get; set; }
        public double effMassZ { get; set; }
        public double effMassXX { get; set; }
        public double effMassYY { get; set; }
        public double effMassZZ { get; set; }
        public ResultGlobal() { }

        [SchemaInfo("ResultGlobal (load case)", "Creates a Speckle global result object (for load case)", "Structural", "Results")]
        public ResultGlobal(LoadCase resultCase, double loadX, double loadY, double loadZ, double loadXX, double loadYY, double loadZZ, double reactionX, double reactionY, double reactionZ, double reactionXX, double reactionYY, double reactionZZ, double mode, double frequency, double loadFactor, double modalStiffness, double modalGeoStiffness, double effMassX, double effMassY, double effMassZ, double effMassXX, double effMassYY, double effMassZZ)
        {            
            this.resultCase = resultCase;
            this.loadX = loadX;
            this.loadY = loadY;
            this.loadZ = loadZ;
            this.loadXX = loadXX;
            this.loadYY = loadYY;
            this.loadZZ = loadZZ;
            this.reactionX = reactionX;
            this.reactionY = reactionY;
            this.reactionZ = reactionZ;
            this.reactionXX = reactionXX;
            this.reactionYY = reactionYY;
            this.reactionZZ = reactionZZ;
            this.mode = mode;
            this.frequency = frequency;
            this.loadFactor = loadFactor;
            this.modalStiffness = modalStiffness;
            this.modalGeoStiffness = modalGeoStiffness;
            this.effMassX = effMassX;
            this.effMassY = effMassY;
            this.effMassZ = effMassZ;
            this.effMassXX = effMassXX;
            this.effMassYY = effMassYY;
            this.effMassZZ = effMassZZ;
        }

        [SchemaInfo("ResultGlobal (load combination)", "Creates a Speckle global result object (for load combination)", "Structural", "Results")]
        public ResultGlobal(LoadCombination resultCase, double loadX, double loadY, double loadZ, double loadXX, double loadYY, double loadZZ, double reactionX, double reactionY, double reactionZ, double reactionXX, double reactionYY, double reactionZZ, double mode, double frequency, double loadFactor, double modalStiffness, double modalGeoStiffness, double effMassX, double effMassY, double effMassZ, double effMassXX, double effMassYY, double effMassZZ)
        {
            this.resultCase = resultCase;
            this.loadX = loadX;
            this.loadY = loadY;
            this.loadZ = loadZ;
            this.loadXX = loadXX;
            this.loadYY = loadYY;
            this.loadZZ = loadZZ;
            this.reactionX = reactionX;
            this.reactionY = reactionY;
            this.reactionZ = reactionZ;
            this.reactionXX = reactionXX;
            this.reactionYY = reactionYY;
            this.reactionZZ = reactionZZ;
            this.mode = mode;
            this.frequency = frequency;
            this.loadFactor = loadFactor;
            this.modalStiffness = modalStiffness;
            this.modalGeoStiffness = modalGeoStiffness;
            this.effMassX = effMassX;
            this.effMassY = effMassY;
            this.effMassZ = effMassZ;
            this.effMassXX = effMassXX;
            this.effMassYY = effMassYY;
            this.effMassZZ = effMassZZ;
        }
    }
}
