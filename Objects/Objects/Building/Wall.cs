using System.Collections.Generic;
using Objects.Geometry;
using Objects.Organization;
using Speckle.Core.Models;

namespace Objects.Building
{
  /// <summary>
  /// The basic Speckle <see cref="Wall"/> class.
  /// A Speckle <see cref="Wall"/> can be defined by
  /// <list type="number">
  /// <item>A <see cref="CurveBasedElement.baseCurve"/> and a <see cref="height"/></item>
  /// <item>
  ///   A <see cref="CurveBasedElement.baseCurve"/> and two levels (the <see cref="baseLevel"/> and <see cref="topLevel"/>)
  ///   and optionally two offsets (<see cref="baseOffset"/> and <see cref="topOffset"/>)
  /// </item>
  /// </list>
  /// </summary>
  public class Wall : CurveBasedElement
  {
    /// <summary>
    /// The height of this <see cref="Wall"/> measured in the specified <see cref="CurveBasedElement.units"/>
    /// </summary>
    public double height { get; set; }

    /// <summary>
    /// True if this <see cref="Wall"/> orientation is flipped
    /// </summary>
    public bool flipped { get; set; }

    /// <summary>
    /// The bottom (base) level of this <see cref="Wall"/>
    /// </summary>
    public Level baseLevel { get; set; }

    /// <summary>
    /// The top level of this <see cref="Wall"/>.
    /// </summary>
    public Level topLevel { get; set; }

    /// <summary>
    /// The offset of this <see cref="Wall"/> from the <see cref="baseLevel"/>
    /// </summary>
    public double baseOffset { get; set; }

    /// <summary>
    /// The offset of this <see cref="Wall"/> from the <see cref="topLevel"/>
    /// </summary>
    public double topOffset { get; set; }


    public Wall()
    {
    }
    

  }
}