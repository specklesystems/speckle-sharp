using System.Collections.Generic;
using Objects.Geometry;
using Objects.Organization;
using Objects.Properties;
using Speckle.Core.Kits;
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

    /// <summary>
    /// Create a Speckle <see cref="Wall"/> by <see cref="baseCurve"/> and <see cref="height"/>.
    /// Optionally add the <see cref="baseLevel"/>, whether or not the wall is <see cref="flipped"/>, and any nested <see cref="elements"/>.
    /// </summary>
    /// <param name="height">The height of the Wall. The <see cref="Wall.units"/> are specified by the units of the defining <see cref="baseCurve"/></param>
    /// <param name="baseCurve">The <see cref="ICurve"/> defining the base of this Wall. The units of this curve define the units of this Wall.</param>
    /// <param name="flipped">[Optional] Whether or not this Wall is flipped (defaults to false)</param>
    /// <param name="baseLevel">[Optional] The base level of this Wall</param>
    /// <param name="elements">[Optional] Any nested elements this wall might have as a list of <see cref="Base"/> objects</param>
    /// <param name="sourceApp">[Optional] Source <see cref="ApplicationProperties"/> for higher fidelity data transfer to the target application</param>
    [SchemaInfo("Wall by curve and height", "Creates a Speckle wall.", "BIM", "Architecture")]
    public Wall(double height, [SchemaMainParam]  ICurve baseCurve, bool flipped = false, Level baseLevel = null,
      [SchemaParamInfo("Any nested elements that this wall might have")]
      List<Base> elements = null,
      [SchemaParamInfo("Source application properties for higher fidelity data transfer to the target application")]
      ApplicationProperties sourceApp = null)
    {
      this.height = height;
      this.baseCurve = baseCurve;

      this.flipped = flipped;
      this.elements = elements;
      this.baseLevel = baseLevel;
      this.sourceApp = sourceApp;
    }

    /// <summary>
    /// Create a Speckle <see cref="Wall"/> by <see cref="baseCurve"/> and levels (<see cref="baseLevel"/> and <see cref="topLevel"/>).
    /// Optionally add base and top offsets, whether or not the Wall is flipped, and any nested <see cref="elements"/>.
    /// </summary>
    /// <param name="baseCurve">The <see cref="ICurve"/> defining the base of this Wall. The units of this curve define the units of this Wall.</param>
    /// <param name="baseLevel">The base <see cref="Level"/> of this Wall</param>
    /// <param name="topLevel">The top <see cref="Level"/> of this Wall</param>
    /// <param name="baseOffset">[Optional] The offset from the <see cref="baseLevel"/> (defaults to 0)</param>
    /// <param name="topOffset">[Optional] The offset from the <see cref="topLevel"/> (defaults to 0)</param>
    /// <param name="flipped">[Optional] Whether or not this Wall is flipped (defaults to false)</param>
    /// <param name="elements">[Optional] Any nested elements this wall might have as a list of <see cref="Base"/> objects</param>
    /// <param name="sourceApp">[Optional] Source <see cref="ApplicationProperties"/> for higher fidelity data transfer to the target application</param>
    [SchemaInfo("Wall by curve and levels", "Creates a Speckle wall.", "BIM", "Architecture")]
    public Wall([SchemaMainParam] ICurve baseCurve, Level baseLevel, Level topLevel, double baseOffset = 0,
      double topOffset = 0, bool flipped = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")]
      List<Base> elements = null,
      [SchemaParamInfo("Source application properties for higher fidelity data transfer to the target application")]
      ApplicationProperties sourceApp = null)
    {
      this.baseCurve = baseCurve;
      this.baseLevel = baseLevel;
      this.topLevel = topLevel;
      
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.elements = elements;
      this.sourceApp = sourceApp;
    }
  }
}