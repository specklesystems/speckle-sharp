using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitFaceWall : Wall
{
  public RevitFaceWall() { }

  [SchemaInfo("RevitWall by face", "Creates a Revit wall from a surface.", "Revit", "Architecture")]
  public RevitFaceWall(
    string family,
    string type,
    [SchemaParamInfo("Surface or single face Brep to use"), SchemaMainParam] Brep surface,
    Level level,
    LocationLine locationLine = LocationLine.Interior,
    [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
  {
    if (surface.Surfaces.Count == 0)
    {
      throw new Exception("Cannot create a RevitWall with an empty BREP");
    }

    if (surface.Surfaces.Count > 1)
    {
      throw new Exception(
        "The provided brep has more than 1 surface. Please deconstruct/explode it to create multiple instances"
      );
    }

    this.family = family;
    this.type = type;
    brep = surface;
    this.locationLine = locationLine;
    this.level = level;
    this.elements = elements;
    this.parameters = parameters?.ToBase();
  }

  public string family { get; set; }
  public string type { get; set; }

  public Brep brep { get; set; }

  public new Level? level
  {
    get => base.level;
    set => base.level = value;
  }

  public LocationLine locationLine { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }
}
