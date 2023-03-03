﻿using System;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Wall : Base, IDisplayValue<List<Mesh>>
  {
    public double height { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }
    public ICurve baseLine { get; set; }
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Wall() { }

    /// <summary>
    /// SchemaBuilder constructor for a Speckle wall
    /// </summary>
    /// <param name="height"></param>
    /// <param name="baseLine"></param>
    /// <param name="elements"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("Wall", "Creates a Speckle wall", "BIM", "Architecture")]
    public Wall(double height, [SchemaMainParam] ICurve baseLine,
      [SchemaParamInfo("Any nested elements that this wall might have")] List<Base> elements = null)
    {
      this.height = height;
      this.baseLine = baseLine;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitWall : Wall
  {
    public string family { get; set; }
    public string type { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public bool flipped { get; set; }
    public bool structural { get; set; }
    public Level level { get; set; }
    public Level topLevel { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitWall() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit wall
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="baseLine"></param>
    /// <param name="level"></param>
    /// <param name="topLevel"></param>
    /// <param name="baseOffset"></param>
    /// <param name="topOffset"></param>
    /// <param name="flipped"></param>
    /// <param name="structural"></param>
    /// <param name="elements"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="baseOffset"/> and <paramref name="topOffset"/> params</remarks>
    [SchemaInfo("RevitWall by curve and levels", "Creates a Revit wall with a top and base level.", "Revit", "Architecture")]
    public RevitWall(string family, string type,
      [SchemaMainParam] ICurve baseLine, Level level, Level topLevel, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.structural = structural;
      this.level = level;
      this.topLevel = topLevel;
      this.elements = elements;
      this.parameters = parameters.ToBase();
    }

    /// <summary>
    /// SchemaBuilder constructor for a Revit wall
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="baseLine"></param>
    /// <param name="level"></param>
    /// <param name="height"></param>
    /// <param name="baseOffset"></param>
    /// <param name="topOffset"></param>
    /// <param name="flipped"></param>
    /// <param name="structural"></param>
    /// <param name="elements"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/>, <paramref name="baseOffset"/>, and <paramref name="topOffset"/> params</remarks>
    [SchemaInfo("RevitWall by curve and height", "Creates an unconnected Revit wall.", "Revit", "Architecture")]
    public RevitWall(string family, string type,
      [SchemaMainParam] ICurve baseLine, Level level, double height, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.height = height;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.structural = structural;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters.ToBase();
    }
  }

  public class RevitFaceWall : Wall
  {
    public string family { get; set; }
    public string type { get; set; }
    public Surface surface { get; set; }
    public Level level { get; set; }
    public LocationLine locationLine { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitFaceWall() { }

    [SchemaInfo("RevitWall by face", "Creates a Revit wall from a surface.", "Revit", "Architecture")]
    public RevitFaceWall(string family, string type,
      [SchemaParamInfo("Surface or single face Brep to use")][SchemaMainParam] Brep surface,
      Level level, LocationLine locationLine = LocationLine.Interior,
      [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      
      this.family = family;
      this.type = type;
      this.surface = surface?.Surfaces[0];
      this.locationLine = locationLine;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters.ToBase();
    }
  }

  public class RevitProfileWall : Wall
  {
    public string family { get; set; }
    public string type { get; set; }
    public Polycurve profile { get; set; }
    public Level level { get; set; }
    public LocationLine locationLine { get; set; }
    public bool structural { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitProfileWall() { }

    [SchemaInfo("RevitWall by profile", "Creates a Revit wall from a profile.", "Revit", "Architecture")]
    public RevitProfileWall(string family, string type,
      [SchemaParamInfo("Profile to use")][SchemaMainParam] Polycurve profile, Level level,
      LocationLine locationLine = LocationLine.Interior, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.profile = profile;
      this.locationLine = locationLine;
      this.structural = structural;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters.ToBase();
    }
  }


  // [SchemaDescription("Not supported yet.")]
  // [SchemaIgnore]
  // public class RevitCurtainWall : Wall
  // {
  //   // TODO
  //   // What props do/can curtain walls have? - grid, mullions, etc.
  //
  //   [SchemaOptional]
  //   public bool flipped { get; set; }
  //
  //   [SchemaOptional]
  //   public Base parameters { get; set; }
  //
  //   [SchemaIgnore]
  //   public string elementId { get; set; }
  // }
  //
  // [SchemaDescription("Not supported yet.")]
  // [SchemaIgnore]
  // public class RevitWallByPoint : Base
  // {
  //   [SchemaOptional]
  //   public Base parameters { get; set; }
  //
  //   [SchemaIgnore]
  //   public string elementId { get; set; }
  // }
}


namespace Objects.BuiltElements.Archicad
{
  public class ArchicadWall : Objects.BuiltElements.Wall
  {
    // Wall geometry
    public int? floorIndex { get; set; }

    public double baseOffset { get; set; }

    public Point startPoint { get; set; }

    public Point endPoint { get; set; }

    public string structure { get; set; }

    public string geometryMethod { get; set; }

    public string wallComplexity { get; set; }

    public string? buildingMaterialName { get; set; }

    public string? compositeName { get; set; }

    public string? profileName { get; set; }

    public double? arcAngle { get; set; }

    public ElementShape? shape { get; set; }

    public double thickness { get; set; }

    public double? outsideSlantAngle { get; set; }

    public double? insideSlantAngle { get; set; }

    public bool? polyWalllCornersCanChange { get; set; }

    // Wall and stories relation
    public double topOffset { get; set; }

    public short relativeTopStory { get; set; }

    public string referenceLineLocation { get; set; }

    public double? referenceLineOffset { get; set; }

    public double offsetFromOutside { get; set; }

    public int referenceLineStartIndex { get; set; }

    public int referenceLineEndIndex { get; set; }

    public bool flipped { get; set; }

    // Floor Plan and Section - Floor Plan Display
    public string showOnStories { get; set; }

    public string displayOptionName { get; set; }

    public string showProjectionName { get; set; }

    // Floor Plan and Section - Cut Surfaces parameters
    public short? cutLinePen { get; set; }

    public string? cutLinetype { get; set; }

    public short? overrideCutFillPen { get; set; }

    public short? overrideCutFillBackgroundPen { get; set; }

    // Floor Plan and Section - Outlines parameters
    public short uncutLinePen { get; set; }

    public string uncutLinetype { get; set; }

    public short overheadLinePen { get; set; }

    public string overheadLinetype { get; set; }

    // Model - Override Surfaces
    public string? referenceMaterialName { get; set; }

    public int? referenceMaterialStartIndex { get; set; }

    public int? referenceMaterialEndIndex { get; set; }

    public string? oppositeMaterialName { get; set; }

    public int? oppositeMaterialStartIndex { get; set; }

    public int? oppositeMaterialEndIndex { get; set; }

    public string? sideMaterialName { get; set; }

    public bool materialsChained { get; set; }

    public bool inheritEndSurface { get; set; }

    public bool alignTexture { get; set; }

    public int sequence { get; set; }

    // Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
    public double? logHeight { get; set; }

    public bool? startWithHalfLog { get; set; }

    public string? surfaceOfHorizontalEdges { get; set; }

    public string? logShape { get; set; }

    // Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
    public string wallRelationToZoneName { get; set; }

    // Does it have any embedded object?
    public bool hasDoor { get; set; }

    public bool hasWindow { get; set; }

    public ArchicadWall() { }
    public ArchicadWall(Point startPoint, Point endPoint, double height, bool flipped = false)
    {
      this.startPoint = startPoint;
      this.endPoint = endPoint;
      this.height = height;
      this.flipped = flipped;
    }
  }

}
