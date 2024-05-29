using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitProfileWall : Wall
{
  public RevitProfileWall() { }

  public RevitProfileWall(
    string family,
    string type,
    Polycurve profile,
    Level level,
    double height,
    string units,
    string? elementId,
    LocationLine locationLine = LocationLine.Interior,
    bool structural = false,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
    : base(height, units, profile, level, displayValue, elements)
  {
    this.family = family;
    this.profile = profile; //TODO: IS the profile the same as baseLine?
    this.type = type;
    this.locationLine = locationLine;
    this.structural = structural;
    this.elementId = elementId;
    this.parameters = parameters?.ToBase();
  }

  public string family { get; set; }
  public string type { get; set; }
  public Polycurve profile { get; set; }

  public new Level? level
  {
    get => base.level;
    set => base.level = value;
  }

  public LocationLine locationLine { get; set; }
  public bool structural { get; set; }
  public Base? parameters { get; set; }
  public string? elementId { get; set; }

  #region Scehma Info Ctors

  [SchemaInfo("RevitWall by profile", "Creates a Revit wall from a profile.", "Revit", "Architecture")]
  public RevitProfileWall(
    string family,
    string type,
    [SchemaParamInfo("Profile to use"), SchemaMainParam] Polycurve profile,
    Level level,
    LocationLine locationLine = LocationLine.Interior,
    bool structural = false,
    [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
    : this(
      family,
      type,
      profile,
      level,
      0,
      null,
      null,
      locationLine,
      structural,
      elements: elements,
      parameters: parameters
    ) { }

  #endregion
}
