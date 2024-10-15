using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Results;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.CSI.Geometry;

public class CSIElement1D : Element1D
{
  public CSIElement1D(Line baseLine, Property1D property, ElementType1D type)
    : base(baseLine, property, type, null, null, null, null, null, null, null) { }

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
  [
    SchemaInfo("Element1D (from local axis)", "Creates a Speckle CSI 1D element (from local axis)", "CSI", "Geometry"),
    SchemaDeprecated
  ]
  public CSIElement1D(
    Line baseLine,
    Property1D property,
    ElementType1D type,
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Plane? localAxis = null,
    CSILinearSpring? CSILinearSpring = null,
    [SchemaParamInfo("An array of 8 values referring to the modifiers as seen in CSI in order")]
      double[]? Modifier = null,
    DesignProcedure DesignProcedure = DesignProcedure.NoDesign
  )
    : base(baseLine, property, type, name, end1Releases, end2Releases, end1Offset, end2Offset, localAxis)
  {
    this.CSILinearSpring = CSILinearSpring;
    this.DesignProcedure = DesignProcedure;
    StiffnessModifiers = Modifier.ToList();
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
  [SchemaInfo(
    "Element1D (from orientation node and angle)",
    "Creates a Speckle CSI 1D element (from orientation node and angle)",
    "CSI",
    "Geometry"
  )]
  public CSIElement1D(
    Line baseLine,
    Property1D property,
    ElementType1D type,
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Node? orientationNode = null,
    double orientationAngle = 0,
    CSILinearSpring? CSILinearSpring = null,
    [SchemaParamInfo("A list of 8 values referring to the modifiers as seen in CSI in order")]
      List<double>? Modifier = null,
    DesignProcedure DesignProcedure = DesignProcedure.NoDesign
  )
    : base(
      baseLine,
      property,
      type,
      name,
      end1Releases,
      end2Releases,
      end1Offset,
      end2Offset,
      orientationNode,
      orientationAngle
    )
  {
    this.CSILinearSpring = CSILinearSpring;
    this.DesignProcedure = DesignProcedure;
    StiffnessModifiers = Modifier;
  }

  [SchemaInfo("Element1D (from local axis)", "Creates a Speckle CSI 1D element (from local axis)", "CSI", "Geometry")]
  public CSIElement1D(
    Line baseLine,
    Property1D property,
    ElementType1D type,
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Plane? localAxis = null,
    CSILinearSpring? CSILinearSpring = null,
    [SchemaParamInfo("A list of 8 values referring to the modifiers as seen in CSI in order")]
      List<double>? Modifier = null,
    DesignProcedure DesignProcedure = DesignProcedure.NoDesign
  )
    : base(baseLine, property, type, name, end1Releases, end2Releases, end1Offset, end2Offset, localAxis)
  {
    this.CSILinearSpring = CSILinearSpring;
    this.DesignProcedure = DesignProcedure;
    StiffnessModifiers = Modifier;
  }

  public CSIElement1D() { }

  [DetachProperty]
  public CSILinearSpring? CSILinearSpring { get; set; }

  public string PierAssignment { get; set; }
  public string SpandrelAssignment { get; set; }

  [JsonIgnore, Obsolete("This is changed to a list of doubles for Grasshopper compatibility")]
  public double[]? Modifiers
  {
    get => StiffnessModifiers?.ToArray();
    set => StiffnessModifiers = value?.ToList();
  }
  public List<double>? StiffnessModifiers { get; set; }
  public DesignProcedure DesignProcedure { get; set; }

  [DetachProperty]
  public AnalyticalResults? AnalysisResults { get; set; }
}
