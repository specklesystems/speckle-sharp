using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.Results
{
    public enum CaseType
    {
        Analysis,
        Combination
    }

    public class ResultSetNode : Result
    {
        [DetachProperty]
        public List<ResultNode> resultsNode { get; set; }
        public ResultSetNode() { }

        [SchemaInfo("ResultSetNode", "Creates a Speckle node result set object", "Structural", "Results")]
        public ResultSetNode(List<ResultNode> resultsNode)
        {
            this.resultsNode = resultsNode;
        }
    }

    public class ResultNode : Result
    {
        [DetachProperty]
        public Node node { get; set; }
        public double dispX { get; set; }
        public double dispY { get; set; }
        public double dispZ { get; set; }
        public double rotXX { get; set; }
        public double rotYY { get; set; }
        public double rotZZ { get; set; }
        public double reactionX { get; set; }
        public double reactionY { get; set; }
        public double reactionZ { get; set; }
        public double reactionXX { get; set; }
        public double reactionYY { get; set; }
        public double reactionZZ { get; set; }
        public double constraintX { get; set; }
        public double constraintY { get; set; }
        public double constraintZ { get; set; }
        public double constraintXX { get; set; }
        public double constraintYY { get; set; }
        public double constraintZZ { get; set; }
        public double velX { get; set; }
        public double velY { get; set; }
        public double velZ { get; set; }
        public double velXX { get; set; }
        public double velYY { get; set; }
        public double velZZ { get; set; }
        public double accX { get; set; }
        public double accY { get; set; }
        public double accZ { get; set; }
        public double accXX { get; set; }
        public double accYY { get; set; }
        public double accZZ { get; set; }
        public ResultNode() { }

        [SchemaInfo("ResultNode (load case)", "Creates a Speckle structural nodal result object", "Structural", "Results")]
        public ResultNode(LoadCase resultCase, Node node, double dispX, double dispY, double dispZ, double rotXX, double rotYY, double rotZZ, double reactionX, double reactionY, double reactionZ, double reactionXX, double reactionYY, double reactionZZ, double constraintX, double constraintY, double constraintZ, double constraintXX, double constraintYY, double constraintZZ, double velX, double velY, double velZ, double velXX, double velYY, double velZZ, double accX, double accY, double accZ, double accXX, double accYY, double accZZ)
        {
            this.resultCase = resultCase;
            this.node = node;
            this.dispX = dispX;
            this.dispY = dispY;
            this.dispZ = dispZ;
            this.rotXX = rotXX;
            this.rotYY = rotYY;
            this.rotZZ = rotZZ;
            this.reactionX = reactionX;
            this.reactionY = reactionY;
            this.reactionZ = reactionZ;
            this.reactionXX = reactionXX;
            this.reactionYY = reactionYY;
            this.reactionZZ = reactionZZ;
            this.constraintX = constraintX;
            this.constraintY = constraintY;
            this.constraintZ = constraintZ;
            this.constraintXX = constraintXX;
            this.constraintYY = constraintYY;
            this.constraintZZ = constraintZZ;
            this.velX = velX;
            this.velY = velY;
            this.velZ = velZ;
            this.velXX = velXX;
            this.velYY = velYY;
            this.velZZ = velZZ;
            this.accX = accX;
            this.accY = accY;
            this.accZ = accZ;
            this.accXX = accXX;
            this.accYY = accYY;
            this.accZZ = accZZ;
        }

        [SchemaInfo("ResultNode (load combination)", "Creates a Speckle structural nodal result object", "Structural", "Results")]
        public ResultNode(LoadCombination resultCase, Node node, double dispX, double dispY, double dispZ, double rotXX, double rotYY, double rotZZ, double reactionX, double reactionY, double reactionZ, double reactionXX, double reactionYY, double reactionZZ, double constraintX, double constraintY, double constraintZ, double constraintXX, double constraintYY, double constraintZZ, double velX, double velY, double velZ, double velXX, double velYY, double velZZ, double accX, double accY, double accZ, double accXX, double accYY, double accZZ)
        {
            this.resultCase = resultCase;
            this.node = node;
            this.dispX = dispX;
            this.dispY = dispY;
            this.dispZ = dispZ;
            this.rotXX = rotXX;
            this.rotYY = rotYY;
            this.rotZZ = rotZZ;
            this.reactionX = reactionX;
            this.reactionY = reactionY;
            this.reactionZ = reactionZ;
            this.reactionXX = reactionXX;
            this.reactionYY = reactionYY;
            this.reactionZZ = reactionZZ;
            this.constraintX = constraintX;
            this.constraintY = constraintY;
            this.constraintZ = constraintZ;
            this.constraintXX = constraintXX;
            this.constraintYY = constraintYY;
            this.constraintZZ = constraintZZ;
            this.velX = velX;
            this.velY = velY;
            this.velZ = velZ;
            this.velXX = velXX;
            this.velYY = velYY;
            this.velZZ = velZZ;
            this.accX = accX;
            this.accY = accY;
            this.accZ = accZ;
            this.accXX = accXX;
            this.accYY = accYY;
            this.accZZ = accZZ;
        }
    }
}
