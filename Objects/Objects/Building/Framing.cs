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
  /// 

  public interface IFraming
  {
    /// <summary>
    /// The  (base) level of this <see cref="Framing"/>
    /// </summary>
    Level baseLevel { get; set; }


    /// <summary>
    /// The offset of the Framing
    /// </summary>
    double offset { get; set; }
  }
  public class Column : CurveBasedElement, IFraming
  {
    public Column()
    {
    }

    public Level baseLevel { get; set; }
    public double offset { get; set; }
  }
  public class Beam : CurveBasedElement, IFraming
  {
    public Level baseLevel { get; set; }
    public double offset { get; set; }
    public Beam()
    {

    }
  }
  public class Brace : CurveBasedElement, IFraming
  {
    public Level baseLevel { get; set; }
    public double offset { get; set; }
    public Brace()
    {

    }
  }
}
