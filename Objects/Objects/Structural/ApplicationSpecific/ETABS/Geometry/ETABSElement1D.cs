using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using Objects.Structural.ETABS.Properties;
using Speckle.Core.Kits;
using Objects.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.ETABS.Geometry
{
  public class ETABSElement1D: Element1D
  {
   public ETABSLinearSpring ETABSLinearSpring { get; set; }
   public string PierAssignment { get; set; }
   public string SpandrelAssignment { get; set; }

    /// <summary>
    /// SchemaBuilder constructor for structural 1D element (based on local axis)
    /// </summary>
    /// <param name="baseLine"></param>
    /// <param name="property"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="end1Releases"></param>
    /// <param name="end2Releases"></param>
    /// <param name="end1Offset"></param>
    /// <param name="end2Offset"></param>
    /// <param name="localAxis"></param>
    [SchemaInfo("Element1D (from local axis)", "Creates a Speckle structural 1D element (from local axis)", "Structural", "Geometry")]
    public ETABSElement1D(Line baseLine, Property1D property, ElementType1D type,
        string name = null,
        [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end1Releases = null,
        [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end2Releases = null,
        [SchemaParamInfo("If null, defaults to no offsets")] Vector end1Offset = null,
        [SchemaParamInfo("If null, defaults to no offsets")] Vector end2Offset = null, Plane localAxis = null,
        ETABSLinearSpring ETABSLinearSpring = null)
    {
      this.baseLine = baseLine;
      this.property = property;
      this.type = type;
      this.name = name;
      this.end1Releases = end1Releases == null ? new Restraint("FFFFFF") : end1Releases;
      this.end2Releases = end2Releases == null ? new Restraint("FFFFFF") : end2Releases;
      this.end1Offset = end1Offset == null ? new Vector(0, 0, 0) : end1Offset;
      this.end2Offset = end2Offset == null ? new Vector(0, 0, 0) : end2Offset;
      this.localAxis = localAxis;
      this.ETABSLinearSpring = ETABSLinearSpring;
    }

    /// <summary>
    /// SchemaBuilder constructor for structural 1D element (based on orientation node and angle)
    /// </summary>
    /// <param name="baseLine"></param>
    /// <param name="property"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="end1Releases"></param>
    /// <param name="end2Releases"></param>
    /// <param name="end1Offset"></param>
    /// <param name="end2Offset"></param>
    /// <param name="orientationNode"></param>
    /// <param name="orientationAngle"></param>
    [SchemaInfo("Element1D (from orientation node and angle)", "Creates a Speckle structural 1D element (from orientation node and angle)", "Structural", "Geometry")]
    public ETABSElement1D(Line baseLine, Property1D property, ElementType1D type,
         string name = null,
         [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end1Releases = null,
         [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end2Releases = null,
         [SchemaParamInfo("If null, defaults to no offsets")] Vector end1Offset = null,
         [SchemaParamInfo("If null, defaults to no offsets")] Vector end2Offset = null,
         Node orientationNode = null, double orientationAngle = 0,
         ETABSLinearSpring ETABSLinearSpring = null)
    {
      this.baseLine = baseLine;
      this.property = property;
      this.type = type;
      this.name = name;
      this.end1Releases = end1Releases == null ? new Restraint("FFFFFF") : end1Releases;
      this.end2Releases = end2Releases == null ? new Restraint("FFFFFF") : end2Releases;
      this.end1Offset = end1Offset == null ? new Vector(0, 0, 0) : end1Offset;
      this.end2Offset = end2Offset == null ? new Vector(0, 0, 0) : end2Offset;
      this.orientationNode = orientationNode;
      this.orientationAngle = orientationAngle;
      this.ETABSLinearSpring = ETABSLinearSpring;
    }

    public ETABSElement1D()
    {
    }
  }
}
