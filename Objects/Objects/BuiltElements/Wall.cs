using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Wall : Base, IDisplayValue<List<Mesh>>
{
  public Wall() { }

  /// <summary>
  /// SchemaBuilder constructor for a Speckle wall
  /// </summary>
  /// <param name="height"></param>
  /// <param name="baseLine"></param>
  /// <param name="elements"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
  [SchemaInfo("Wall", "Creates a Speckle wall", "BIM", "Architecture")]
  public Wall(
    double height,
    [SchemaMainParam] ICurve baseLine,
    [SchemaParamInfo("Any nested elements that this wall might have")] List<Base>? elements = null
  )
  {
    this.height = height;
    this.baseLine = baseLine;
    this.elements = elements;
  }

  public double height { get; set; }

  [DetachProperty]
  public List<Base>? elements { get; set; }

  public ICurve baseLine { get; set; }
  public virtual Level? level { get; internal set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
