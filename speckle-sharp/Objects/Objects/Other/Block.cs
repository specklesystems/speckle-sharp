using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Logging;

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
    [Obsolete("Use GetInsertionPoint method")]
    public Point insertionPoint { get => GetInsertionPoint(); set { } }

    /// <summary>
    /// The 4x4 transform matrix.
    /// </summary>
    /// <remarks>
    /// the 3x3 sub-matrix determines scaling
    /// the 4th column defines translation, where the last value could be a divisor
    /// </remarks>
    public double[] transform { get; set; }

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
      var (x, y, z, u) = blockDefinition.basePoint;
      var insertion = new double[] { x, y, z, 1 };

      if (transform.Length != 16)
        throw new SpeckleException($"{nameof(BlockInstance)}.{nameof(transform)} is malformed: expected length to be 4x4 = 16");

      for (int i = 0; i < 4; i++)
        for (int j = 0; j < 16; j+= 4)
          insertion[i] = insertion[i] * (transform[j] + transform[j + 1] + transform[j + 2] + transform[j + 3]);
      
      return new Point(
        insertion[0] / insertion[3], 
        insertion[1] / insertion[3], 
        insertion[2] / insertion[3], 
        u);
    }
  }
}
