using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

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
        new ButtonSetting {Slug = "mapping", Name = "Custom Type Mappings", Icon ="ChartTimelineVarient", ButtonText="Not Set"},
      };
    }

    public override List<ISetting> GetSettings(StreamState state, ProgressViewModel progress)
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
        new ButtonSetting {Slug = "mapping", Name = "Custom Type Mappings", Icon ="ChartTimelineVarient", ButtonText="Not Set", state=state, progress=progress},
      };
    }

    public override List<string> GetHostProperties()
    {
      var list = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(FamilySymbol));
      List<string> familyType = list.Select(o => o.Name).Distinct().ToList();
      return familyType;
    }

    public override async Task<Dictionary<string,string>> GetInitialMapping(StreamState state, ProgressViewModel progress, List<string> hostProperties)
    {
      List<Base> flattenedBase = await GetFlattenedBase(state, progress);
      
      var listProperties = GetListProperties(flattenedBase);
      
      //var listHostProperties = GetHostDocumentPropeties(CurrentDoc.Document);
      
      var mappings = returnFirstPassMap(listProperties, hostProperties);

      return mappings;
      //User to update logic from computer here;

      //var vm = new MappingViewModel(mappings);
      //var mappingView = new MappingView
      //{
      //  DataContext = vm
      //};

      //mappingView.ShowDialog(MainWindow.Instance);
      //vm.OnRequestClose += (s, e) => mappingView.Close();
      //var newMappings = await mappingView.ShowDialog<Dictionary<string, string>?>(MainWindow.Instance);
      //System.Diagnostics.Debug.WriteLine($"new mappings {newMappings}");

      //updateRecieveObject(mappings, objects);

      
    }

    public async Task<List<Base>> GetFlattenedBase(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();

      progress.Report.Log($"kit");
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      progress.Report.Log($"converter");
      converter.SetContextDocument(CurrentDoc.Document);
      var previouslyReceiveObjects = state.ReceivedObjects;
      progress.Report.Log($"previouslyReceiveObjects");

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);
      progress.Report.Log($"settings set");

      var transport = new ServerTransport(state.Client.Account, state.StreamId);
      progress.Report.Log($"transport");

      var stream = await state.Client.StreamGet(state.StreamId);
      progress.Report.Log($"stream");

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }
      progress.Report.Log($"no cancel");

      Commit myCommit = null;

      progress.Report.Log($"state");

      // always get latest stream
      //var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
      //myCommit = res.commits.items.FirstOrDefault();

      var res = await state.Client.StreamGetCommits(state.StreamId, 1);
      myCommit = res.FirstOrDefault();

      progress.Report.Log($"res {res}");

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

      progress.Report.Log("commitObject");

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

      
      //if (progress.Report.OperationErrorsCount != 0)
      //{
      //  return state;
      //}

      //if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      //{
      //  return null;
      //}

      var flattenedObjects = FlattenCommitObject(commitObject, converter);
      return flattenedObjects;
    }

    private List<string> GetListProperties(List<Base> objects)
    {
      List<string> listProperties = new List<string> { };
      foreach (var @object in objects)
      {
        try
        {
          //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
          var propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
          listProperties.Add(propInfo);
        }
        catch
        {

        }

      }
      return listProperties.Distinct().ToList();
    }

    private List<string> GetHostDocumentPropeties(Document doc)
    {
      var list = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));
      List<string> familyType = list.Select(o => o.Name).Distinct().ToList();
      return familyType;
    }

    public static int LevenshteinDistance(string s, string t)
    {
      // Default algorithim for computing the similarity between strings
      int n = s.Length;
      int m = t.Length;
      int[,] d = new int[n + 1, m + 1];
      if (n == 0)
      {
        return m;
      }
      if (m == 0)
      {
        return n;
      }
      for (int i = 0; i <= n; d[i, 0] = i++)
        ;
      for (int j = 0; j <= m; d[0, j] = j++)
        ;
      for (int i = 1; i <= n; i++)
      {
        for (int j = 1; j <= m; j++)
        {
          int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
          d[i, j] = Math.Min(
              Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
              d[i - 1, j - 1] + cost);
        }
      }
      return d[n, m];
    }

    public Dictionary<string, string> returnFirstPassMap(List<string> specklePropertyList, List<string> hostPropertyList)
    {
      var mappings = new Dictionary<string, string> { };
      foreach (var item in specklePropertyList)
      {
        List<int> listVert = new List<int> { };
        foreach (var hostItem in hostPropertyList)
        {
          listVert.Add(LevenshteinDistance(item, hostItem));
        }
        var indexMin = listVert.IndexOf(listVert.Min());
        mappings.Add(item, hostPropertyList[indexMin]);
      }
      return mappings;
    }

    public void updateRecieveObject(Dictionary<string, string> Map, List<Base> objects)
    {
      foreach (var @object in objects)
      {

        try
        {
          //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
          var propInfo = "";
          propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
          if (propInfo != "")
          {
            string mappingProperty = "";
            Map.TryGetValue(propInfo, out mappingProperty);
            var prop = @object.GetType().GetProperty("type");
            prop.SetValue(@object, mappingProperty);
          }
        }
        catch
        {

        }
      }
    }
  }
}
