using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Revit.Async;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
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

    const string noMapping = "None";
    const string everyReceive = "Every Receive";
    const string forNewTypes = "When Unmapped Types are Detected";

    public override List<ISetting> GetSettings()
    {
      List<string> referencePoints = new List<string>() { InternalOrigin };
      List<string> prettyMeshOptions = new List<string>() { defaultValue, dxf, familyDxf };

      // find project base point and survey point. these don't always have name props, so store them under custom strings
      var basePoint = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(BasePoint)).Cast<BasePoint>().Where(o => o.IsShared == false).FirstOrDefault();
      if (basePoint != null)
        referencePoints.Add(ProjectBase);
      var surveyPoint = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(BasePoint)).Cast<BasePoint>().Where(o => o.IsShared == true).FirstOrDefault();
      if (surveyPoint != null)
        referencePoints.Add(Survey);

      return new List<ISetting>
      {
        new ListBoxSetting {Slug = "reference-point", Name = "Reference Point", Icon ="LocationSearching", Values = referencePoints, Description = "Sends or receives stream objects in relation to this document point"},
        new CheckBoxSetting {Slug = "linkedmodels-send", Name = "Send Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models in the selection filters when sending"},
        new CheckBoxSetting {Slug = "linkedmodels-receive", Name = "Receive Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models when receiving NOTE: elements from linked models will be received in the current document"},
        new MultiSelectBoxSetting { Slug = "disallow-join", Name = "Disallow Join For Elements", Icon = "CallSplit", Description = "Determine which objects should not be allowed to join by default when receiving",
          Values = new List<string>() { ArchitecturalWalls, StructuralWalls, StructuralFraming } },
        new ListBoxSetting {Slug = "pretty-mesh", Name = "Mesh Import Method", Icon ="ChartTimelineVarient", Values = prettyMeshOptions, Description = "Determines the display style of imported meshes"},
        new CheckBoxSetting{Slug = "recieve-mappings" , Name = "Toggle for Mappings", Icon = "Link", IsChecked = false,Description = "If toggled, map on recieve of Objects" },
        //new MappingSeting {Slug = "mapping", Name = "Custom Type Mappings", Icon ="ChartTimelineVarient", ButtonText="Not Set"},
      };
    }

    public override List<ISetting> GetSettings(StreamState state, ProgressViewModel progress)
    {
      List<string> referencePoints = new List<string>() { InternalOrigin };
      List<string> prettyMeshOptions = new List<string>() { defaultValue, dxf, familyDxf };
      List<string> mappingOptions = new List<string>() { noMapping, everyReceive, forNewTypes };

      // find project base point and survey point. these don't always have name props, so store them under custom strings
      var basePoint = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(BasePoint)).Cast<BasePoint>().Where(o => o.IsShared == false).FirstOrDefault();
      if (basePoint != null)
        referencePoints.Add(ProjectBase);
      var surveyPoint = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(BasePoint)).Cast<BasePoint>().Where(o => o.IsShared == true).FirstOrDefault();
      if (surveyPoint != null)
        referencePoints.Add(Survey);

      return new List<ISetting>
      {
        new ListBoxSetting {Slug = "reference-point", Name = "Reference Point", Icon ="LocationSearching", Values = referencePoints, Description = "Sends or receives stream objects in relation to this document point"},
        new CheckBoxSetting {Slug = "linkedmodels-send", Name = "Send Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models in the selection filters when sending"},
        new CheckBoxSetting {Slug = "linkedmodels-receive", Name = "Receive Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models when receiving NOTE: elements from linked models will be received in the current document"},
        new MultiSelectBoxSetting { Slug = "disallow-join", Name = "Disallow Join For Elements", Icon = "CallSplit", Description = "Determine which objects should not be allowed to join by default when receiving",
          Values = new List<string>() { ArchitecturalWalls, StructuralWalls, StructuralFraming } },
        new ListBoxSetting {Slug = "pretty-mesh", Name = "Mesh Import Method", Icon ="ChartTimelineVarient", Values = prettyMeshOptions, Description = "Determines the display style of imported meshes"},
        new MappingSeting {Slug = "recieve-mappings", Name = "Custom Type Mapping", Icon ="LocationSearching", Values = mappingOptions, Description = "Sends or receives stream objects in relation to this document point"},
        //new CheckBoxSetting{Slug = "recieve-mappings" , Name = "Toggle for Mappings", Icon = "Link", IsChecked = false,Description = "If toggled, map on recieve of Objects" },
        //new ButtonSetting {Slug = "mapping", Name = "Custom Type Mappings", Icon ="ChartTimelineVarient", ButtonText="Set", state=state, progress=progress},
      };
    }

    public override List<string> GetHostProperties()
    {
      ElementClassFilter familySymbolFilter = new ElementClassFilter(typeof(FamilySymbol));
      ElementClassFilter wallTypeFilter = new ElementClassFilter(typeof(WallType));
      LogicalOrFilter filter = new LogicalOrFilter(familySymbolFilter, wallTypeFilter);
      //var list = new FilteredElementCollector(CurrentDoc.Document).WherePasses(filter);

      var list = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(FamilySymbol));
      List<string> familyType = list.Select(o => o.Name).Distinct().ToList();
      return familyType;
    }

    public override Dictionary<string, List<string>> GetHostTypes()
    {
      var returnDict = new Dictionary<string, List<string>>();
      var collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
      List<ElementId> exclusionFilterIds = new List<ElementId>();

      FilteredElementCollector list = null;
      List<string> types = null;

      // Materials
      list = collector.OfClass(typeof(Autodesk.Revit.DB.Material));
      types = list.Select(o => o.Name).Distinct().ToList();
      exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
      returnDict["Materials"] = types;

      // Floors
      collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
      list = collector.OfClass(typeof(FloorType));
      types = list.Select(o => o.Name).Distinct().ToList();
      exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
      returnDict["Floors"] = types;

      // Walls
      collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
      list = collector.OfClass(typeof(WallType));
      types = list.Select(o => o.Name).Distinct().ToList();
      exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
      returnDict["Walls"] = types;

      // Framing
      collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
      list = collector.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming);
      types = list.Select(o => o.Name).Distinct().ToList();
      exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
      returnDict["Framing"] = types;

      // Columns
      collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
      var filter = new ElementMulticategoryFilter(new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns });
      list = collector.OfClass(typeof(FamilySymbol)).WherePasses(filter);
      types = list.Select(o => o.Name).Distinct().ToList();
      exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
      returnDict["Columns"] = types;

      // Misc
      collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
      list = collector.Excluding(exclusionFilterIds);
      types = list.Select(o => o.Name).Distinct().ToList();
      returnDict["Miscellaneous"] = types;

      return returnDict;
    }

    public async Task<List<Base>> GetFlattenedBase(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);
      var previouslyReceiveObjects = state.ReceivedObjects;

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      Commit myCommit = null;

      // always get latest stream
      var res = await state.Client.StreamGetCommits(state.StreamId, 1);
      myCommit = res.FirstOrDefault();

      string referencedObject = myCommit.referencedObject;

      var commitObject = await Operations.Receive(
          referencedObject,
          progress.CancellationTokenSource.Token,
          transport,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: (s, e) =>
          {
            progress.Report.LogOperationError(e);
            progress.CancellationTokenSource.Cancel();
          },
          onTotalChildrenCountKnown: count => { progress.Max = count; },
          disposeTransports: true
          );

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = myCommit?.id,
          message = myCommit?.message,
          sourceApplication = ConnectorRevitUtils.RevitAppName
        });
      }
      catch
      {
        // Do nothing!
      }

      var flattenedObjects = FlattenCommitObject(commitObject, converter);
      return flattenedObjects;
    }

    //private List<string> GetListProperties(List<Base> objects)
    //{
    //  List<string> listProperties = new List<string> { };
    //  foreach (var @object in objects)
    //  {
    //    try
    //    {
    //      //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
    //      var propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
    //      listProperties.Add(propInfo);
    //    }
    //    catch
    //    {

    //    }

    //  }
    //  return listProperties.Distinct().ToList();
    //}

    private Dictionary<string,List<string>> GetListProperties(List<Base> objects, ProgressViewModel progress)
    {
      progress.Report.Log($"get list props");
      var returnDict = new Dictionary<string, List<string>>();

      foreach (var @object in objects)
      {
        try
        {
          //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
          var type = @object.GetType().GetProperty("type").GetValue(@object) as string;
          string speckleType = null;

          try
          {
            speckleType = @object.speckle_type.Split(':')[0];
          }
          catch
          {
            speckleType = @object.speckle_type;
          }

          string typeCategory = "";

          switch (speckleType)
          {
            case "Objects.BuiltElements.Floor":
              typeCategory = "Floors";
              break;
            case "Objects.BuiltElements.Wall":
              typeCategory = "Walls";
              break;
            case "Objects.BuiltElements.Beam":
              typeCategory = "Framing";
              break;
            case "Objects.BuiltElements.Column":
              typeCategory = "Columns";
              break ;
            default:
              typeCategory = "Miscellaneous";
              break;
          }

          if (returnDict.ContainsKey(typeCategory))
          {
            returnDict[typeCategory].Add(type);
          }
          else
          {
            returnDict[typeCategory] = new List<string> { type };
          }

          // try to get the material
          if (@object["materialQuantites"] is List<Base> mats)
          {
            foreach (var mat in mats)
            {
              if (mat["material"] is Base b)
              {
                if (b["name"] is string matName)
                {
                  if (returnDict.ContainsKey("Materials"))
                  {
                    returnDict["Materials"].Add(matName);
                  }
                  else
                  {
                    returnDict["Materials"] = new List<string> { matName };
                  }
                }
              }
            }
          }
        }
        catch
        {

        }
      }

      var newDictionary = returnDict.ToDictionary(entry => entry.Key, entry => entry.Value);
      foreach (var key in newDictionary.Keys)
      {
        progress.Report.Log($"key {key} value {String.Join(",", returnDict[key])}");
        returnDict[key] = returnDict[key].Distinct().ToList();
      }
      return returnDict;
    }

    public string GetTypeCategory(Base obj)
    {
      string speckleType = null;

      try
      {
        speckleType = obj.speckle_type.Split(':')[0];
      }
      catch
      {
        speckleType = obj.speckle_type;
      }

      string typeCategory = "";

      switch (speckleType)
      {
        case "Objects.BuiltElements.Floor":
          typeCategory = "Floors";
          break;
        case "Objects.BuiltElements.Wall":
          typeCategory = "Walls";
          break;
        case "Objects.BuiltElements.Beam":
          typeCategory = "Framing";
          break;
        case "Objects.BuiltElements.Column":
          typeCategory = "Columns";
          break;
        default:
          typeCategory = "Miscellaneous";
          break;
      }
      return typeCategory;
    }

    //private List<string> GetHostDocumentPropeties(Document doc)
    //{
    //  var list = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));
    //  List<string> familyType = list.Select(o => o.Name).Distinct().ToList();
    //  return familyType;
    //}
  }
}
