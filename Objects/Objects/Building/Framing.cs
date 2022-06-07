using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Objects.Organization;

namespace Objects.Building
{
  /// <summary>
  /// The basic Speckle <see cref="Framing"/> class.
  /// A Speckle <see cref="Framing"/> can be defined by
  /// <list type="number">
  /// <item>A <see cref="CurveBasedElement.baseCurve"/> and a <see cref="height"/></item>
  /// <item>
  ///   
  /// </item>
  /// </list>
  /// </summary>
  public class Framing : CurveBasedElement
  {


    /// <summary>
    /// The  (base) level of this <see cref="Framing"/>
    /// </summary>
    public Level baseLevel { get; set; }


    /// <summary>
    /// The offset of this <see cref="Framing"/> from the end of Point3
    /// </summary>
    public Vector end1Offset { get; set; }

    /// <summary>
    /// The offset of this <see cref="Framing"/> from the end of Point2
    /// </summary>
    public Vector end2Offset { get; set; }

    public FramingType framingType { get; set; } = FramingType.Beam;

      public enum FramingType
    {
      Column,
      Brace,
      Beam
    }
    public Framing()
    {
    }  
  }
}
