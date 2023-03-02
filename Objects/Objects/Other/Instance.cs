using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

using Objects.Geometry;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Plane = Objects.Geometry.Plane;
using Vector = Objects.Geometry.Vector;

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

    /// <summary>
    /// The units of this Instance, should be the same as the instance transform units
    /// </summary>
    public string units { get; set; } 

    [DetachProperty]
    public virtual Base definition { get; set; }

    public Instance () { }
  }

  /// <summary>
  /// Block instance class 
  /// </summary>
  public class BlockInstance : Instance
  {
    [JsonIgnore, Obsolete("Use GetInsertionPoint method"), SchemaIgnore]
    public Point insertionPoint { get => GetInsertionPoint(); set { } }

    /// <inheritdoc cref="GetTransformedGeometry"/>
    [JsonIgnore]
    public List<ITransformable> transformedGeometry => GetTransformedGeometry();

    /// <inheritdoc cref="GetInsertionPlane"/>
    [JsonIgnore]
    public Plane insertionPlane => GetInsertionPlane();

    [DetachProperty]
    [Obsolete("Use definition property")]
    public BlockDefinition blockDefinition { get; set; }

    public override Base definition
    {
      get
      {
        return blockDefinition;
      }
      set
      {
        if (value is BlockDefinition)
        {
          blockDefinition = (BlockDefinition)value;
        }
      }
    }

    public BlockInstance() { }

    [SchemaInfo("Block Instance", "A Speckle Block Instance")]
    public BlockInstance(BlockDefinition blockDefinition, Transform transform)
    {
      this.definition = blockDefinition;
      this.transform = transform;

      // OLD: TODO: need to verify
      // Add base translation to transform. This assumes the transform is based on the world origin,
      // whereas the instance transform assumes it contains the basePoint translation already.
      //this.transform = transform * blockDefinition.GetBasePointTransform();
    }

    /// <summary>
    /// Retrieves Instance insertion point by applying <see cref="transform"/> to <see cref="BlockDefinition.basePoint"/>
    /// </summary>
    /// <returns>Insertion point as a <see cref="Point"/></returns>
    public Point GetInsertionPoint()
    {
      var newMatrix = transform.matrix;
      newMatrix.M14 -= Convert.ToSingle(blockDefinition.basePoint.x);
      newMatrix.M24 -= Convert.ToSingle(blockDefinition.basePoint.y);
      newMatrix.M34 -= Convert.ToSingle(blockDefinition.basePoint.z);
      blockDefinition.basePoint.TransformTo(new Transform(newMatrix, units), out Point insertionPoint);
      return insertionPoint;
    }

    /// <summary>
    /// Returns the a copy of the Block Definition's geometry transformed with this BlockInstance's transform.
    /// </summary>
    /// <returns>The transformed geometry for this BlockInstance.</returns>
    public List<ITransformable> GetTransformedGeometry()
    {
      return blockDefinition.geometry.SelectMany(b =>
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
      // TODO: UPDATE!
      var plane = new Plane(blockDefinition.basePoint, new Vector(0, 0, 1, units), new Vector(1, 0, 0, units), new Vector(0, 1, 0, units), units);
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }

  }
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

    public override Base definition { 
      get 
      {
        return elementDefinition;
      }
      set 
      {
        if (value is RevitSymbolElementType)
        {
          elementDefinition = (RevitSymbolElementType)value;
        }
      }
    }

    [JsonIgnore]
    private RevitSymbolElementType elementDefinition { get; set; }

    public RevitInstance() { }

  }
}
