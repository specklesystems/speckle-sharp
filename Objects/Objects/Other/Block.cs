using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;

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
    public Point insertionPoint { get; set; }

    /// <summary>
    /// The 4x4 transform matrix. Row-dominant, should have 16 values.
    /// </summary>
    public double[] transform { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public BlockDefinition blockDefinition { get; set; }

    public BlockInstance() { }
  }
}
