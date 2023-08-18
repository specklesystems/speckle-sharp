using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorRevit.UI
{
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

    public override List<ISetting> GetSettings()
    {
      List<string> referencePoints = new List<string>() { InternalOrigin };
      List<string> prettyMeshOptions = new List<string>() { defaultValue, dxf, familyDxf };
      List<string> mappingOptions = new List<string>() { noMapping, everyReceive, forNewTypes };

      // find project base point and survey point. these don't always have name props, so store them under custom strings
      var basePoint = new FilteredElementCollector(CurrentDoc.Document)
        .OfClass(typeof(BasePoint))
        .Cast<BasePoint>()
        .FirstOrDefault(o => !o.IsShared);
      if (basePoint != null)
        referencePoints.Add(ProjectBase);
      var surveyPoint = new FilteredElementCollector(CurrentDoc.Document)
        .OfClass(typeof(BasePoint))
        .Cast<BasePoint>()
        .FirstOrDefault(o => o.IsShared);
      if (surveyPoint != null)
        referencePoints.Add(Survey);

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
          Slug = "direct-shape-strategy",
          Name = "Convert received objects to DirectShape",
          Icon = "Link",
          Values = new List<string> { "Always", "On Error", "Never" },
          Selection = "On Error",
          Description = "Determines when to attempt conversion of an element into a DirectShape"
        },
        new MultiSelectBoxSetting
        {
          Slug = "disallow-join",
          Name = "Disallow Join For Elements",
          Icon = "CallSplit",
          Description = "Determine which objects should not be allowed to join by default when receiving",
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
          Name = "Custom Type Mapping",
          Icon = "LocationSearching",
          Values = mappingOptions,
          Description = "Determine how incoming object types are mapped to object types in the host application"
        },
      };
    }
  }
}
