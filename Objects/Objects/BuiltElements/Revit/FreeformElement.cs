using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Revit;

public class FreeformElement : Base, IDisplayValue<List<Base>>
{
  public FreeformElement() { }

  [SchemaInfo(
    "Freeform element",
    "Creates a Revit Freeform element using a list of Brep or Meshes. Category defaults to Generic Models",
    "Revit",
    "Families"
  )]
  public FreeformElement(List<Base> baseGeometries, string subcategory = "", List<Parameter>? parameters = null)
  {
    this.baseGeometries = baseGeometries;
    //this.category = category;
    this.subcategory = subcategory;
    if (!IsValid())
    {
      throw new Exception("Freeform elements can only be created from BREPs or Meshes");
    }

    this.parameters = parameters?.ToBase();
  }

  public Base? parameters { get; set; }

  public string subcategory { get; set; }

  public string elementId { get; set; }

  /// <summary>
  /// DEPRECATED. Sets the geometry contained in the FreeformElement. This field has been deprecated in favor of `baseGeometries`
  /// to align with Revit's API. It remains as a setter-only property for backwards compatibility.
  /// It will set the first item on the baseGeometries list, and instantiate a list if necessary.
  /// </summary>
  [JsonIgnore, SchemaIgnore, Obsolete("Use 'baseGeometries' instead", true)]
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1044:Properties should not be write only",
    Justification = "Obsolete"
  )]
  public Base baseGeometry
  {
    set
    {
      if (baseGeometries == null)
      {
        baseGeometries = new List<Base> { value };
      }
      else if (baseGeometries.Count == 0)
      {
        baseGeometries.Add(value);
      }
      else
      {
        baseGeometries[0] = value;
      }
    }
  }

  [DetachProperty, Chunkable]
  public List<Base> baseGeometries { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Base> displayValue { get; set; }

  public bool IsValid()
  {
    return baseGeometries.All(IsValidObject);
  }

  public bool IsValidObject(Base @base)
  {
    return @base is Mesh || @base is Brep || @base is Geometry.Curve;
  }

  #region Deprecated Constructors

  [
    SchemaDeprecated,
    SchemaInfo(
      "Freeform element",
      "Creates a Revit Freeform element using a list of Brep or Meshes.",
      "Revit",
      "Families"
    )
  ]
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2201:Do not raise reserved exception types",
    Justification = "Obsolete"
  )]
  public FreeformElement(Base baseGeometry, List<Parameter>? parameters = null)
  {
    if (!IsValidObject(baseGeometry))
    {
      throw new Exception("Freeform elements can only be created from BREPs or Meshes");
    }

    baseGeometries = new List<Base> { baseGeometry };
    this.parameters = parameters?.ToBase();
  }

  [
    SchemaDeprecated,
    SchemaInfo(
      "Freeform element",
      "Creates a Revit Freeform element using a list of Brep or Meshes.",
      "Revit",
      "Families"
    )
  ]
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2201:Do not raise reserved exception types",
    Justification = "Obsolete"
  )]
  public FreeformElement(List<Base> baseGeometries, List<Parameter>? parameters = null)
  {
    this.baseGeometries = baseGeometries;
    if (!IsValid())
    {
      throw new Exception("Freeform elements can only be created from BREPs or Meshes");
    }

    this.parameters = parameters?.ToBase();
  }

  #endregion
}
