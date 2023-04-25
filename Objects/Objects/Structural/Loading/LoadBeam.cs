using System.Collections.Generic;
using Objects.Structural.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Loading;

public class LoadBeam : Load
{
  public LoadBeam() { }

  /// <summary>
  /// A beam load (for 1D elements)
  /// </summary>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="elements">A list of 1D elements to apply the load to</param>
  /// <param name="loadType">The type of loading applied</param>
  /// <param name="direction">The direction of the load, with respect to the specified axis</param>
  /// <param name="loadAxis">The axis in which the direction of the load is defined</param>
  /// <param name="values">The magnitude of the load, either a force or moment</param>
  /// <param name="positions">The locations of the load</param>
  /// <param name="isProjected">Whether the load is projected (ie. whether the distributed load is specified as the intensity applied to the projection of the element on the surface normal to the direction of the load, like snow in an inclined roof)</param>
  /// <param name="name">A name or description to identify the load</param>
  [SchemaInfo("Beam Load", "Creates a Speckle structural beam (1D elem/member) load", "Structural", "Loading")]
  public LoadBeam(
    LoadCase loadCase,
    List<Base> elements,
    BeamLoadType loadType,
    LoadDirection direction,
    LoadAxisType loadAxis = LoadAxisType.Global,
    [SchemaParamInfo(
      "A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)"
    )]
      List<double> values = null,
    [SchemaParamInfo(
      "A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)"
    )]
      List<double> positions = null,
    bool isProjected = false,
    string name = null
  )
  {
    this.loadCase = loadCase;
    this.elements = elements;
    this.loadType = loadType;
    this.direction = direction;
    loadAxisType = loadAxis;
    this.values = values;
    this.positions = positions;
    this.isProjected = isProjected;
    this.name = name;
  }

  /// <summary>
  /// A beam load (for 1D elements) with a user-defined axis
  /// </summary>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="elements">A list of 1D elements to apply the load to</param>
  /// <param name="loadType">The type of loading applied</param>
  /// <param name="direction">The direction of the load, with respect to the specified axis</param>
  /// <param name="loadAxis">The axis in which the direction of the load is defined (can be a user-defined axis)</param>
  /// <param name="values">The magnitude of the load, either a force or moment</param>
  /// <param name="positions">The locations of the load</param>
  /// <param name="isProjected">Whether the load is projected (ie. whether the distributed load is specified as the intensity applied to the projection of the element on the surface normal to the direction of the load, like snow in an inclined roof)</param>
  /// <param name="name">A name or description to identify the load</param>
  [SchemaInfo(
    "Beam Load (user-defined axis)",
    "Creates a Speckle structural beam (1D elem/member) load (specified using a user-defined axis)",
    "Structural",
    "Loading"
  )]
  public LoadBeam(
    LoadCase loadCase,
    List<Base> elements,
    BeamLoadType loadType,
    LoadDirection direction,
    Axis loadAxis,
    [SchemaParamInfo(
      "A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)"
    )]
      List<double> values = null,
    [SchemaParamInfo(
      "A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)"
    )]
      List<double> positions = null,
    bool isProjected = false,
    string name = null
  )
  {
    this.loadCase = loadCase;
    this.elements = elements;
    this.loadType = loadType;
    this.direction = direction;
    this.values = values;
    this.positions = positions;
    this.isProjected = isProjected;
    this.name = name;
  }

  [DetachProperty, Chunkable(5000)]
  public List<Base> elements { get; set; }

  public BeamLoadType loadType { get; set; }
  public LoadDirection direction { get; set; }

  [DetachProperty]
  public Axis loadAxis { get; set; }

  public LoadAxisType loadAxisType { get; set; }
  public bool isProjected { get; set; }
  public List<double> values { get; set; }
  public List<double> positions { get; set; }
}
