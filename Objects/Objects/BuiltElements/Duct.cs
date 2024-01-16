using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements;

public class Duct : Base, IDisplayValue<List<Mesh>>
{
  public Duct() { }

  /// <summary>
  /// SchemaBuilder constructor for a Speckle duct
  /// </summary>
  /// <param name="baseLine"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <param name="diameter"></param>
  /// <param name="velocity"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="width"/>, <paramref name="height"/>, and <paramref name="diameter"/> params</remarks>
  [SchemaInfo("Duct", "Creates a Speckle duct", "BIM", "MEP"), SchemaDeprecated]
  public Duct([SchemaMainParam] Line baseLine, double width, double height, double diameter, double velocity = 0)
  {
    baseCurve = baseLine;
    this.width = width;
    this.height = height;
    this.diameter = diameter;
    this.velocity = velocity;
  }

  /// <summary>
  /// SchemaBuilder constructor for a Speckle duct
  /// </summary>
  /// <param name="baseCurve"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <param name="diameter"></param>
  /// <param name="velocity"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="width"/>, <paramref name="height"/>, and <paramref name="diameter"/> params</remarks>
  [SchemaInfo("Duct", "Creates a Speckle duct", "BIM", "MEP")]
  public Duct([SchemaMainParam] ICurve baseCurve, double width, double height, double diameter, double velocity = 0)
  {
    this.baseCurve = baseCurve;
    this.width = width;
    this.height = height;
    this.diameter = diameter;
    this.velocity = velocity;
  }

  [JsonIgnore, Obsolete("Replaced with baseCurve property", true)]
  public Line baseLine { get; set; }

  public ICurve baseCurve { get; set; }
  public double width { get; set; }
  public double height { get; set; }
  public double diameter { get; set; }
  public double length { get; set; }
  public double velocity { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
