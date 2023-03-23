using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DesktopUI2.Models;
using DesktopUI2.Views;
using Objects.BuiltElements.Revit;
using ReactiveUI;
using Serilog;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels.MappingTool
{
  public class MappingsViewModel : ViewModelBase, IScreen
  {
    public string TitleFull => "Speckle Mappings";
    public RoutingState Router { get; private set; }

    public MappingsBindings Bindings { get; private set; } = new DummyMappingsBindings();

    private List<Schema> _schemas = new List<Schema>();
    public List<Schema> Schemas
    {
      get => _schemas;
      set => this.RaiseAndSetIfChanged(ref _schemas, value);

    }

    private Schema _selectedSchema;

    public Schema SelectedSchema
    {
      get => _selectedSchema;
      set => this.RaiseAndSetIfChanged(ref _selectedSchema, value);

    }

    public List<RevitElementType> AvailableRevitTypes { get; private set; } = new List<RevitElementType>();
    public List<string> AvailableRevitLevels { get; private set; } = new List<string>();



    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    public static MappingsViewModel Instance { get; private set; }



    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
    }

    private int _count;
    public int Count
    {
      get => _count;
      private set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    private string _promptMsg = "To get started, select a mapping data source.";
    public string PromptMsg
    {
      get => _promptMsg;
      private set => this.RaiseAndSetIfChanged(ref _promptMsg, value);
    }

    private StreamAccountWrapper _selectedStream;
    public StreamAccountWrapper SelectedStream
    {
      get => _selectedStream;
      private set => this.RaiseAndSetIfChanged(ref _selectedStream, value);
    }

    private List<SchemaGroup> _existingSchemas;
    public List<SchemaGroup> ExistingSchemas
    {
      get => _existingSchemas;
      private set => this.RaiseAndSetIfChanged(ref _existingSchemas, value);
    }

    public StreamSelectorViewModel StreamSelector { get; private set; } = new StreamSelectorViewModel();

    MappingSelectionInfo _lastInfo = null;
    public MappingsViewModel()
    {
      Init();
    }

    public MappingsViewModel(MappingsBindings bindings)
    {
      Bindings = bindings;
      Init();
    }


    public void Init()
    {
      Instance = this;
      Router = new RoutingState();
      Bindings.UpdateSelection = UpdateSelection;
      Bindings.UpdateExistingSchemaElements = UpdateExistingSchemaElements;
      RouterInstance = Router; // makes the router available app-wide


      UpdateSelection(Bindings.GetSelectionInfo());
      UpdateExistingSchemaElements(Bindings.GetExistingSchemaElements());

    }

    private void UpdateSelection(MappingSelectionInfo info)
    {
      _lastInfo = info;
      Count = info.Count;

      PromptMsg = "";

      //empty selection
      if (Count == 0)
      {
        PromptMsg = "Select one or more elements.";
        SelectedSchema = null;
        Schemas = new List<Schema>();
        return;
      }


      //this might be a bit inefficient
      AddRevitInfoToSchema(info.Schemas);

      if (Schemas.Any())
      {
        if (Schemas.Any(x => x.HasData))
          SelectedSchema = Schemas.First(x => x.HasData);
        SelectedSchema = Schemas.First();

        if (SelectedStream == null || !AvailableRevitTypes.Any() || !AvailableRevitLevels.Any())
          PromptMsg = "Try selecting a compatible mapping source for more options.";
      }
      else
      {
        if (SelectedStream == null)
          PromptMsg = "No options available for the current selection, try selecting a mapping data source.";
        else if (!AvailableRevitTypes.Any())
          PromptMsg = "The selected branch does not contain any Revit types, try changing mapping data source.";
        else if (!AvailableRevitLevels.Any())
          PromptMsg = "The selected branch does not contain any Revit levels, try changing mapping data source.";
        else
          PromptMsg = "Incompatible selection, try selecting objects of the same type or changing mapping source";

        SelectedSchema = null;
        return;
      }


    }

    private void UpdateExistingSchemaElements(List<Schema> schemas)
    {
      ExistingSchemas = schemas.GroupBy(x => x.Name).Select(x => new SchemaGroup(x.Key, x.ToList())).ToList();
    }

    internal async void OnBranchSelected()
    {
      try
      {

        var model = await GetCommit();

        if (model == null)
          return;

        GetTypesAndLevels(model);

        SelectedStream = StreamSelector.SelectedStream;

        //force an update
        if (_lastInfo != null)
          UpdateSelection(_lastInfo);

      }
      catch (Exception e)
      {

      }
      finally
      {
        ShowProgress = false;
      }
    }

    private async Task<Base> GetCommit()
    {
      if (StreamSelector.SelectedBranch == null || StreamSelector.SelectedBranch.commits == null || StreamSelector.SelectedBranch.commits.items.Count == 0)
        return null;

      ShowProgress = true;

      var client = new Client(StreamSelector.SelectedStream.Account);
      string referencedObject = StreamSelector.SelectedBranch.commits.items.FirstOrDefault().referencedObject;

      var transport = new ServerTransport(StreamSelector.SelectedStream.Account, StreamSelector.SelectedStream.Stream.id);
      return await Operations.Receive(
          referencedObject,
          transport,
          disposeTransports: true
          );
    }

    private void GetTypesAndLevels(Base model)
    {
      var revitTypes = new List<RevitElementType>();
      try
      {
        var types = model["Types"] as Base;

        foreach (var baseCategory in types.GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic))
        {
          try
          {
            var elementTypes = (baseCategory.Value as List<object>).Cast<RevitElementType>().ToList();
            if (!elementTypes.Any())
              continue;

            revitTypes.AddRange(elementTypes);

          }
          catch (Exception ex)
          {
            continue;
          }
        }
        AvailableRevitTypes = revitTypes;
        AvailableRevitLevels = (model["@Levels"] as List<object>).Cast<RevitLevel>().Select(x => x.name).ToList();

      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Could not get types and levels: {exceptionMessage}", ex.Message);
        return;
      }

    }

    /// <summary>
    /// Add Revit information such as families and levels to our schema to populate dropdowns and such
    /// </summary>
    /// <param name="schemas">Available schemas for the current selection</param>
    private void AddRevitInfoToSchema(List<Schema> schemas)
    {
      try
      {
        //remove duplicates
        schemas = schemas.GroupBy(x => x.Name, (key, g) => g.First()).ToList();
        var updatedSchemas = new List<Schema>();

        foreach (var schema in schemas)
        {

          //no need to do extra stuff
          if (
            schema is DirectShapeFreeformViewModel ||
            schema is RevitTopographyViewModel ||
            schema is RevitDefaultWallViewModel ||
            schema is RevitDefaultBeamViewModel ||
            schema is RevitDefaultBraceViewModel ||
            schema is RevitDefaultColumnViewModel ||
            schema is RevitDefaultPipeViewModel ||
            schema is RevitDefaultDuctViewModel)
          {
            updatedSchemas.Add(schema);
          }
          //add revit info
          else
          {
            switch (schema)
            {
              case RevitWallViewModel o:
                var wallFamilies = AvailableRevitTypes.Where(x => x.category == "Walls").ToList();
                if (!wallFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var wallFamiliesViewModels = wallFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
                o.Families = wallFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitFloorViewModel o:
                var floorFamilies = AvailableRevitTypes.Where(x => x.category == "Floors").ToList();
                if (!floorFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var floorFamiliesViewModels = floorFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
                o.Families = floorFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitBeamViewModel o:
                var beamFamilies = AvailableRevitTypes.Where(x => x.category == "Structural Framing").ToList();
                if (!beamFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var beamFamiliesViewModels = beamFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
                o.Families = beamFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitBraceViewModel o:
                var braceFamilies = AvailableRevitTypes.Where(x => x.category == "Structural Framing").ToList();
                if (!braceFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var braceFamiliesViewModels = braceFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
                o.Families = braceFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitColumnViewModel o:
                var columnFamilies = AvailableRevitTypes.Where(x => x.category == "Structural Columns").ToList();
                if (!columnFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var columnFamiliesViewModels = columnFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
                o.Families = columnFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitPipeViewModel o:
                var pipeFamilies = AvailableRevitTypes.Where(x => x is RevitMepElementType && x.category == "Pipes").Cast<RevitMepElementType>().ToList();
                if (!pipeFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var pipeFamiliesViewModels = pipeFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList(), g.First().shape)).ToList();
                o.Families = pipeFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitDuctViewModel o:
                var ductFamilies = AvailableRevitTypes.Where(x => x is RevitMepElementType && x.category == "Ducts").Cast<RevitMepElementType>().ToList();
                if (!ductFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var ductFamiliesViewModels = ductFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList(), g.First().shape)).ToList();
                o.Families = ductFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;

              case RevitFamilyInstanceViewModel o:
                var fiFamilies = AvailableRevitTypes.Where(x => x is RevitSymbolElementType symbol && symbol.placementType == "OneLevelBased").ToList();
                if (!fiFamilies.Any() || !AvailableRevitLevels.Any())
                  break;
                var fiFamiliesViewModels = fiFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
                o.Families = fiFamiliesViewModels;
                o.Levels = AvailableRevitLevels;
                updatedSchemas.Add(o);
                break;
            }
          }

        }


        //var revitMetadata = new List<RevitMetadataViewModel>();

        ////ADAPTIVE COMPONENT
        //var adaptiveFamilies = revitTypes.Where(x => x.placementType == "Adaptive").ToList();
        //if (adaptiveFamilies.Any())
        //{
        //  revitMetadata.Add(new RevitMetadataViewModel("Adaptive Component", new List<Type> { typeof(AdaptiveComponent) }, adaptiveFamilies));
        //}

        //revitMetadata.Add(new RevitMetadataViewModel("Curve", new List<Type> { typeof(DetailCurve), typeof(ModelCurve), typeof(RoomBoundaryLine), typeof(SpaceSeparationLine) }));
        //revitMetadata.Add(new RevitMetadataViewModel("DirectShape", new List<Type> { typeof(DirectShape) }));

        ////FAMILY INSTANCE
        //var fiFamilies = revitTypes.Where(x => x.placementType != "Adaptive" && x.placementType != "Invalid").ToList();
        //if (fiFamilies.Any())
        //{
        //  revitMetadata.Add(new RevitMetadataViewModel("Family Instance", new List<Type> { typeof(FamilyInstance) }, fiFamilies));
        //}


        //also triggers binding refresh
        Schemas = updatedSchemas;
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Could not add revit info schema: {exceptionMessage}", ex.Message);
      }


    }

    [DependsOn(nameof(SelectedSchema))]
    public bool CanSetMappingsCommand(object parameter)
    {
      if (SelectedSchema == null)
        return false;
      //TODO: trigger refresh as any of these changes so we can check if schema is valid
      //var vals = SelectedSchema.GetType().GetProperties()
      //                      .Select(p => p.GetValue(SelectedSchema)).ToList();
      //bool isAnyPropNull = SelectedSchema.GetType().GetProperties()
      //                      .All(p => p.GetValue(SelectedSchema) != null);


      return true;
    }

    public void SetMappingsCommand()
    {
      Bindings.SetMappings(SelectedSchema.GetSerializedSchema(), SelectedSchema.GetSerializedViewModel());
      Analytics.TrackEvent(Analytics.Events.MappingsAction, new Dictionary<string, object>() { { "name", "Mappings Set" }, { "schema", SelectedSchema.Name } });
    }

    /// <summary>
    /// Returns the ids of the extiging mapping elements that have been checked
    /// A bit hacky but it was complicated to set a Binding working across multiple ListBoxes
    /// </summary>
    /// <returns></returns>
    private List<string> GetCheckedBoxesIds()
    {
      var ids = new List<string>();
      try
      {
        var lBoxes = MappingsControl.Instance.GetVisualDescendants().OfType<ListBox>().Where(x => x.Classes.Contains("ExistingMapping"));
        foreach (var lBox in lBoxes)
        {
          ids.AddRange(lBox.SelectedItems.Cast<Schema>().Where(x => x != null).Select(x => x.ApplicationId));
        }
      }
      catch (Exception ex)
      {
        // fail silently
      }
      return ids;
    }

    public void ClearMappingsCommand()
    {
      Bindings.ClearMappings(GetCheckedBoxesIds());
      Analytics.TrackEvent(Analytics.Events.MappingsAction, new Dictionary<string, object>() { { "name", "Mappings Clear" } });
    }

    public void SelectElementsCommandCommand()
    {
      Bindings.SelectElements(GetCheckedBoxesIds());
      Analytics.TrackEvent(Analytics.Events.MappingsAction, new Dictionary<string, object>() { { "name", "Mappings Select Elements" } });
    }

    public void SelectAllMappingsCommand()
    {
      Bindings.SelectElements(ExistingSchemas.SelectMany(x => x.Schemas.Select(y => y.ApplicationId)).ToList());
      Analytics.TrackEvent(Analytics.Events.MappingsAction, new Dictionary<string, object>() { { "name", "Mappings Select All" } });
    }

    public void OpenGuideCommand()
    {
      Process.Start(
       new ProcessStartInfo($"https://speckle.guide/user/mapping-tool.html") { UseShellExecute = true }
     );
    }
    public void OpenStreamSelectorCommand()
    {
      StreamSelector.IsVisible = true;
    }

    public void FeedbackCommand()
    {
      Process.Start(
       new ProcessStartInfo($"https://speckle.community/t/mapping-tool-for-cad-bim-workflows/4086") { UseShellExecute = true }
     );
    }

  }


}
