using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Other.Revit;

public class RevitInstance : Instance<RevitSymbolElementType>
{
  public Level level { get; set; }
  public bool facingFlipped { get; set; }
  public bool handFlipped { get; set; }
  public bool mirrored { get; set; }
  public Base parameters { get; set; }
  public string elementId { get; set; }

  protected override IEnumerable<Base> GetTransformableGeometry()
  {
    var allChildren = new List<Base>();
    if (typedDefinition.elements != null)
    {
      allChildren.AddRange(typedDefinition.elements);
    }
    if (typedDefinition.displayValue.Count != 0)
    {
      allChildren.AddRange(typedDefinition.displayValue);
    }

    return allChildren;
  }

  [SchemaComputed("transformedGeometry")]
  public override IEnumerable<ITransformable> GetTransformedGeometry()
  {
    var transformed = base.GetTransformedGeometry().ToList();

    // add any dynamically attached elements on this instance
    if ((this["elements"] ?? this["@elements"]) is List<object> elements)
    {
      foreach (var element in elements)
      {
        if (((Base)element)["displayValue"] is List<object> display)
        {
          transformed.AddRange(display.Cast<ITransformable>());
        }
      }
    }

    return transformed;
  }

  /// <summary>
  /// Returns a plane representing the insertion point and orientation of this revit instance.
  /// </summary>
  /// <remarks>This method will skip scaling. If you need scaling, we recommend using the transform instead.</remarks>
  /// <returns>A Plane on the insertion point of this Block Instance, with the correct 3-axis rotations.</returns>
  [SchemaComputed("insertionPlane")]
  public Plane GetInsertionPlane()
  {
    // TODO: Check for Revit in GH/DYN
    var plane = new Plane(
      new Point(0, 0, 0, units),
      new Vector(0, 0, 1, units),
      new Vector(1, 0, 0, units),
      new Vector(0, 1, 0, units),
      units
    );
    plane.TransformTo(transform, out Plane tPlane);
    return tPlane;
  }
}
