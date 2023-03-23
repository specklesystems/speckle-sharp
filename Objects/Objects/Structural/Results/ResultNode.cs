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
    public float? dispX { get; set; }
    public float? dispY { get; set; }
    public float? dispZ { get; set; }
    public float? rotXX { get; set; }
    public float? rotYY { get; set; }
    public float? rotZZ { get; set; }
    public float? reactionX { get; set; }
    public float? reactionY { get; set; }
    public float? reactionZ { get; set; }
    public float? reactionXX { get; set; }
    public float? reactionYY { get; set; }
    public float? reactionZZ { get; set; }
    public float? constraintX { get; set; }
    public float? constraintY { get; set; }
    public float? constraintZ { get; set; }
    public float? constraintXX { get; set; }
    public float? constraintYY { get; set; }
    public float? constraintZZ { get; set; }
    public float? velX { get; set; }
    public float? velY { get; set; }
    public float? velZ { get; set; }
    public float? velXX { get; set; }
    public float? velYY { get; set; }
    public float? velZZ { get; set; }
    public float? accX { get; set; }
    public float? accY { get; set; }
    public float? accZ { get; set; }
    public float? accXX { get; set; }
    public float? accYY { get; set; }
    public float? accZZ { get; set; }
    public ResultNode() { }

    [SchemaInfo("ResultNode (load case)", "Creates a Speckle structural nodal result object", "Structural", "Results")]
    public ResultNode(LoadCase resultCase, Node node, float dispX, float dispY, float dispZ, float rotXX, float rotYY, float rotZZ, float reactionX, float reactionY, float reactionZ, float reactionXX, float reactionYY, float reactionZZ, float constraintX, float constraintY, float constraintZ, float constraintXX, float constraintYY, float constraintZZ, float velX, float velY, float velZ, float velXX, float velYY, float velZZ, float accX, float accY, float accZ, float accXX, float accYY, float accZZ)
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
    public ResultNode(LoadCombination resultCase, Node node, float dispX, float dispY, float dispZ, float rotXX, float rotYY, float rotZZ, float reactionX, float reactionY, float reactionZ, float reactionXX, float reactionYY, float reactionZZ, float constraintX, float constraintY, float constraintZ, float constraintXX, float constraintYY, float constraintZZ, float velX, float velY, float velZ, float velXX, float velYY, float velZZ, float accX, float accY, float accZ, float accXX, float accYY, float accZZ)
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
