using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;

namespace Objects.Other
{
  /// <summary>
  /// Block definition class 
  /// </summary>
  public class BlockDefinition : Base
  {
    public string name { get; set; }
    
    public Point basePoint { get; set; }

    [DetachProperty]
    public List<Base> geometry { get; set; }

    public string units { get; set; }

    public BlockDefinition() { }
    
    [SchemaInfo("Block Definition","A Speckle Block definition")]
    public BlockDefinition(string name, List<Base> geometry, Point basePoint = null )
    {
      this.name = name;
      this.basePoint = basePoint ?? new Point();
      this.geometry = geometry;
    }

    public Transform GetBasePointTransform()
    {
      var transform = new Transform();
      transform.value[3] = -basePoint.x;
      transform.value[7] = -basePoint.y;
      transform.value[12] = -basePoint.z;
      return transform;
    }
  }

  /// <summary>
  /// Block instance class 
  /// </summary>
  public class BlockInstance : Base
  {
    [JsonIgnore, Obsolete("Use GetInsertionPoint method"), SchemaIgnore]
    public Point insertionPoint { get => GetInsertionPoint(); set { } }

    /// <inheritdoc cref="GetTransformedGeometry"/>
    [JsonIgnore]
    public List<ITransformable> transformedGeometry => GetTransformedGeometry();

    /// <inheritdoc cref="GetInsertionPlane"/>
    [JsonIgnore]
    public Plane insertionPlane => GetInsertionPlane();
    
    /// <summary>
    /// The 4x4 transform matrix.
    /// </summary>
    /// <remarks>
    /// the 3x3 sub-matrix determines scaling
    /// the 4th column defines translation, where the last value could be a divisor
    /// </remarks>
    public Transform transform { get; set; } = new Transform();

    public string units { get; set; }

    [DetachProperty]
    public BlockDefinition blockDefinition { get; set; }

    public BlockInstance() { }

    [SchemaInfo("Block Instance", "A Speckle Block Instance")]
    public BlockInstance(BlockDefinition blockDefinition, Transform transform)
    {
      this.blockDefinition = blockDefinition;
      // Add base translation to transform. This assumes the transform is based on the world origin,
      // whereas the instance transform assumes it contains the basePoint translation already.
      this.transform = transform * blockDefinition.GetBasePointTransform();
    }
    /// <summary>
    /// Retrieves Instance insertion point by applying <see cref="transform"/> to <see cref="BlockDefinition.basePoint"/>
    /// </summary>
    /// <returns>Insertion point as a <see cref="Point"/></returns>
    public Point GetInsertionPoint()
    {
      return transform.ApplyToPoint(blockDefinition.basePoint);
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
            return new List<ITransformable>{res ? transformed : null};
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
      var plane = new Plane(blockDefinition.basePoint,new Vector(0,0,1,units),new Vector(1,0,0,units),new Vector(0,1,0,units), units);
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }

  }
}
