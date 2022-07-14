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
  }

  /// <summary>
  /// Block instance class 
  /// </summary>
  public class BlockInstance : Base
  {
    /// <inheritdoc cref="GetInsertionPoint"/>
    [JsonIgnore]
    public Point insertionPoint => GetInsertionPoint();

    /// <inheritdoc cref="GetTransformedGeometry"/>
    [JsonIgnore]
    public List<ITransformable> transformedGeometry => GetTransformedGeometry();



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
      return blockDefinition.geometry.Select(b =>
      {
        if (!(b is ITransformable bt)) return null;
        var res = bt.TransformTo(transform, out ITransformable transformed);
        return res ? transformed : null;
      }).Where(b => b != null).ToList();
    }
  }
}
