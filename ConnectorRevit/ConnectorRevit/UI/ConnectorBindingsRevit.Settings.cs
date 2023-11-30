using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorRevit.UI;

public partial class ConnectorBindingsRevit
{
  // CAUTION: these strings need to have the same values as in the converter
  const string InternalOrigin = "Internal Origin (default)";
  const string ProjectBase = "Project Base";
  const string Survey = "Survey";

  const string defaultValue = "Default";
  const string dxf = "DXF";
  const string familyDxf = "Family DXF";

  const string StructuralFraming = "Structural Framing";
  const string StructuralWalls = "Structural Walls";
  const string ArchitecturalWalls = "Achitectural Walls";

  public const string noMapping = "Never";
  public const string everyReceive = "Always";
  public const string forNewTypes = "For New Types";

  public const string DsFallbackSlug = "direct-shape-strategy";
  public const string DsFallbackOnError = "On Error";
  public const string DsFallbackAways = "Always";
  public const string DsFallbackNever = "Never";

  const string DetailLevelCoarse = "Coarse";
  const string DetailLevelMedium = "Medium";
  const string DetailLevelFine = "Fine";

  public override List<ISetting> GetSettings()
  {
    List<string> referencePoints = new() { InternalOrigin };
    List<string> prettyMeshOptions = new() { defaultValue, dxf, familyDxf };
    List<string> mappingOptions = new() { noMapping, everyReceive, forNewTypes };

    // find project base point and survey point. these don't always have name props, so store them under custom strings
    var basePoint = new FilteredElementCollector(CurrentDoc.Document)
      .OfClass(typeof(BasePoint))
      .Cast<BasePoint>()
      .FirstOrDefault(o => !o.IsShared);
    if (basePoint != null)
    {
      referencePoints.Add(ProjectBase);
    }

    var surveyPoint = new FilteredElementCollector(CurrentDoc.Document)
      .OfClass(typeof(BasePoint))
      .Cast<BasePoint>()
      .FirstOrDefault(o => o.IsShared);
    if (surveyPoint != null)
    {
      referencePoints.Add(Survey);
    }

    return new List<ISetting>
    {
      new ListBoxSetting
      {
        Slug = "reference-point",
        Name = "Reference Point",
        Icon = "LocationSearching",
        Values = referencePoints,
        Selection = InternalOrigin,
        Description = "Sends or receives stream objects in relation to this document point"
      },
      new CheckBoxSetting
      {
        Slug = "linkedmodels-send",
        Name = "Send Linked Models",
        Icon = "Link",
        IsChecked = false,
        Description = "Include Linked Models in the selection filters when sending"
      },
      new CheckBoxSetting
      {
        Slug = "linkedmodels-receive",
        Name = "Receive Linked Models",
        Icon = "Link",
        IsChecked = false,
        Description =
          "Include Linked Models when receiving NOTE: elements from linked models will be received in the current document"
      },
      // new CheckBoxSetting
      // {
      //   Slug = "recieve-objects-mesh",
      //   Name = "Receive Objects as DirectShape",
      //   Icon = "Link",
      //   IsChecked = false,
      //   Description = "Receive the stream as a Meshes only"
      // },
      new ListBoxSetting
      {
        Slug = DsFallbackSlug,
        Name = "Fallback to DirectShape on receive",
        Icon = "Link",
        Values = new List<string> { DsFallbackAways, DsFallbackOnError, DsFallbackNever },
        Selection = DsFallbackOnError,
        Description =
          "Determines when to fallback to DirectShape on receive.\n\nAways: all objects will be received as DirectShapes\nOn Error: only objects that fail or whose types are missing\nNever: disables the fallback behavior"
      },
      new MultiSelectBoxSetting
      {
        Slug = "disallow-join",
        Name = "Disallow Join For Elements",
        Icon = "CallSplit",
        Description = "Determines which objects should not be allowed to join by default when receiving",
        Values = new List<string>() { ArchitecturalWalls, StructuralWalls, StructuralFraming }
      },
      new ListBoxSetting
      {
        Slug = "pretty-mesh",
        Name = "Mesh Import Method",
        Icon = "ChartTimelineVarient",
        Values = prettyMeshOptions,
        Selection = defaultValue,
        Description = "Determines the display style of imported meshes"
      },
      new MappingSetting
      {
        Slug = "receive-mappings",
        Name = "Missing Type Mapping",
        Icon = "LocationSearching",
        Values = mappingOptions,
        Selection = forNewTypes,
        Description =
          "Determines when the missing types dialog is shown\n\nNever: the dialog is never shown\nAlways: the dialog is always shown, useful to edit existing mappings\nFor New Types: the dialog is only shown if there are new unmapped types\n\nNOTE: no dialog is shown if Fallback to DirectShape is set to Always"
      },
      new ListBoxSetting
      {
        Slug = "detail-level",
        Name = "Mesh Export Detail Level (alpha)",
        Icon = "Link",
        Values = new List<string>() { DetailLevelCoarse, DetailLevelMedium, DetailLevelFine },
        Selection = DetailLevelFine,
        Description =
          "Determines the level of detail in which meshes are sent to Speckle. \n\nThis feature is in alpha because primitive objects such as curves, \nwhich are commonly found in coarse or medium detail level element representations, are not supported yet."
      },
    };
  }
}
