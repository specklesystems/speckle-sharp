using System;
using System.Collections.Generic;
using Objects.BuiltElements.Revit.Interfaces;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitDuct : Duct, IHasMEPConnectors
{
  public RevitDuct() { }

  public RevitDuct(
    string family,
    string type,
    ICurve baseCurve,
    string systemName,
    string systemType,
    Level level,
    double width,
    double height,
    double diameter,
    double length,
    string? units,
    string? elementId,
    double velocity = 0,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Parameter>? parameters = null
  )
    : base(baseCurve, width, height, diameter, length, units, velocity, displayValue)
  {
    this.family = family;
    this.type = type;
    this.systemName = systemName;
    this.systemType = systemType;
    this.level = level;
    this.parameters = parameters?.ToBase();
    this.elementId = elementId;
  }

  public string family { get; set; }
  public string type { get; set; }
  public string systemName { get; set; }
  public string systemType { get; set; }
  public Level level { get; set; }
  public Base? parameters { get; set; }
  public string? elementId { get; set; }
  public List<RevitMEPConnector> Connectors { get; set; } = new();

  #region Schema Info Constructors

  [SchemaInfo("RevitDuct (DEPRECATED)", "Creates a Revit duct", "Revit", "MEP")]
  [SchemaDeprecated]
  [Obsolete("Use other Constructor")]
  public RevitDuct(
    string family,
    string type,
    [SchemaMainParam] Line baseLine,
    string systemName,
    string systemType,
    Level level,
    double width,
    double height,
    double diameter,
    double velocity = 0,
    List<Parameter>? parameters = null
  )
  {
    baseCurve = baseLine;
    this.family = family;
    this.type = type;
    this.width = width;
    this.height = height;
    this.diameter = diameter;
    this.velocity = velocity;
    this.systemName = systemName;
    this.systemType = systemType;
    this.parameters = parameters?.ToBase();
    this.level = level;
  }

  [SchemaInfo("RevitDuct", "Creates a Revit duct", "Revit", "MEP")]
  public RevitDuct(
    string family,
    string type,
    [SchemaMainParam] ICurve baseCurve,
    string systemName,
    string systemType,
    Level level,
    double width,
    double height,
    double diameter,
    double velocity = 0,
    List<Parameter>? parameters = null
  )
    : this(
      family,
      type,
      baseCurve,
      systemName,
      systemType,
      level,
      width,
      height,
      diameter,
      default, //TODO: what to do with length?
      null,
      null,
      velocity,
      parameters: parameters
    ) { }

  #endregion
}

public class RevitFlexDuct : RevitDuct
{
  public RevitFlexDuct() { }

  public RevitFlexDuct(
    string family,
    string type,
    [SchemaMainParam] ICurve baseCurve,
    string systemName,
    string systemType,
    Level level,
    double width,
    double height,
    double diameter,
    double length,
    Vector startTangent,
    Vector endTangent,
    string? units,
    string? elementId,
    double velocity = 0,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Parameter>? parameters = null
  )
    : base(
      family,
      type,
      baseCurve,
      systemName,
      systemType,
      level,
      width,
      height,
      diameter,
      length,
      units,
      elementId,
      velocity,
      displayValue,
      parameters
    )
  {
    this.startTangent = startTangent;
    this.endTangent = endTangent;
  }

  public Vector startTangent { get; set; }
  public Vector endTangent { get; set; }

  #region Schema Info Constructor

  [SchemaInfo("RevitFlexDuct", "Creates a Revit flex duct", "Revit", "MEP")]
  public RevitFlexDuct(
    string family,
    string type,
    [SchemaMainParam] ICurve baseCurve,
    string systemName,
    string systemType,
    Level level,
    double width,
    double height,
    double diameter,
    Vector startTangent,
    Vector endTangent,
    double velocity = 0,
    List<Parameter>? parameters = null
  )
    : this(
      family,
      type,
      baseCurve,
      systemName,
      systemType,
      level,
      width,
      height,
      diameter,
      0,
      startTangent,
      endTangent,
      null,
      null,
      velocity,
      parameters: parameters
    ) { }

  #endregion
}
