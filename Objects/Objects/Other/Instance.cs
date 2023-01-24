using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

using Objects.Geometry;
using Objects.BuiltElements;
using Plane = Objects.Geometry.Plane;
using Vector = Objects.Geometry.Vector;
using Objects.BuiltElements.Revit;

namespace Objects.Other
{
  /// <summary>
  /// Generic instance class
  /// </summary>
  public class Instance : Base
  {
    /// <summary>
    /// The column-dominant 4x4 transform of this instance.
    /// </summary>
    /// <remarks>
    /// Indicates transform from internal origin [0,0,0]
    /// </remarks>
    public Transform transform { get; set; } = new Transform();

    [DetachProperty]
    public virtual Base definition { get; set; }

    public Instance () { }
  }

  /*
  public class Block : Base
  {
    public string name { get; set; }

    public Point basePoint { get; set; }

    [DetachProperty]
    public List<Base> geometry { get; set; }

    public Block() { }
  }

  public class BlockInstance : Instance
  {
    /// <inheritdoc cref="GetTransformedGeometry"/>
    [JsonIgnore]
    public List<ITransformable> transformedGeometry => GetTransformedGeometry();

    /// <inheritdoc cref="GetInsertionPlane"/>
    [JsonIgnore]
    public Plane insertionPlane => GetInsertionPlane();

    public string units { get; set; }

    [DetachProperty]
    public Block definition { get; set; }

    public BlockInstance() { }

    public Point GetInsertionPoint() => GetInsertionPoint(definition.basePoint);

    /// <summary>
    /// Returns the a copy of the Block Definition's geometry transformed with this BlockInstance's transform.
    /// </summary>
    /// <returns>The transformed geometry for this BlockInstance.</returns>
    public List<ITransformable> GetTransformedGeometry()
    {
      return block.geometry.SelectMany(b =>
      {
        switch (b)
        {
          case BlockInstance bi:
            return bi.GetTransformedGeometry()?.Select(b =>
            {
              ITransformable childTransformed = null;
              b?.TransformTo(transform, out childTransformed);
              return childTransformed;
            });
          case ITransformable bt:
            var res = bt.TransformTo(transform, out var transformed);
            return new List<ITransformable> { res ? transformed : null };
          default:
            return new List<ITransformable>();
        }
      }).Where(b => b != null).ToList();
    }

    /// <summary>
    /// Returns a plane representing the insertion point and orientation of this Block instance.
    /// </summary>
    /// <remarks>This method will skip scaling. If you need scaling, we recommend using the transform instead.</remarks>
    /// <returns>A Plane on the insertion point of this Block Instance, with the correct 3-axis rotations.</returns>
    public Plane GetInsertionPlane()
    {
      var plane = new Plane(block.basePoint, new Vector(0, 0, 1, units), new Vector(1, 0, 0, units), new Vector(0, 1, 0, units), units);
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }

  }
  */

}

namespace Objects.Other.Revit
{
  public class RevitInstance : Instance
  {
    public Level level { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }

    public override Base definition { 
      get {
        return familyDefinition;
      }
      set 
      {
        if (value is FamilyType)
        {
          familyDefinition = (FamilyType)value;
        }
      }
    }

    [JsonIgnore]
    private FamilyType familyDefinition { get; set; }

    public RevitInstance() { }

  }
}
