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

  /// <summary>
  /// SchemaBuilder constructor for a Revit duct (deprecated)
  /// </summary>
  /// <param name="family"></param>
  /// <param name="type"></param>
  /// <param name="baseLine"></param>
  /// <param name="systemName"></param>
  /// <param name="systemType"></param>
  /// <param name="level"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <param name="diameter"></param>
  /// <param name="velocity"></param>
  /// <param name="parameters"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="width"/>, <paramref name="height"/>, and <paramref name="diameter"/> params</remarks>
  [SchemaInfo("RevitDuct", "Creates a Revit duct", "Revit", "MEP"), SchemaDeprecated]
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

  /// <summary>
  /// SchemaBuilder constructor for a Revit duct
  /// </summary>
  /// <param name="family"></param>
  /// <param name="type"></param>
  /// <param name="baseCurve"></param>
  /// <param name="systemName"></param>
  /// <param name="systemType"></param>
  /// <param name="level"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <param name="diameter"></param>
  /// <param name="velocity"></param>
  /// <param name="parameters"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="width"/>, <paramref name="height"/>, and <paramref name="diameter"/> params</remarks>
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
  {
    this.baseCurve = baseCurve;
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

  public string family { get; set; }
  public string type { get; set; }
  public string systemName { get; set; }
  public string systemType { get; set; }
  public Level level { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }
  public List<RevitMEPConnector> Connectors { get; set; } = new();
}

public class RevitFlexDuct : RevitDuct
{
  public RevitFlexDuct() { }

  /// <summary>
  /// SchemaBuilder constructor for a Revit flex duct
  /// </summary>
  /// <param name="family"></param>
  /// <param name="type"></param>
  /// <param name="baseCurve"></param>
  /// <param name="systemName"></param>
  /// <param name="systemType"></param>
  /// <param name="level"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <param name="diameter"></param>
  /// <param name="velocity"></param>
  /// <param name="parameters"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="width"/>, <paramref name="height"/>, and <paramref name="diameter"/> params</remarks>
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
  {
    this.baseCurve = baseCurve;
    this.family = family;
    this.type = type;
    this.width = width;
    this.height = height;
    this.diameter = diameter;
    this.startTangent = startTangent;
    this.endTangent = endTangent;
    this.velocity = velocity;
    this.systemName = systemName;
    this.systemType = systemType;
    this.parameters = parameters?.ToBase();
    this.level = level;
  }

  public Vector startTangent { get; set; }
  public Vector endTangent { get; set; }
}
