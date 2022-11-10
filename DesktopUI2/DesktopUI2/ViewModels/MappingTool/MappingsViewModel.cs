using Avalonia.Metadata;
using DesktopUI2.Models;
using Objects.BuiltElements.Revit;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

    private List<Type> _applicableSchemas;
    public List<Type> ApplicableSchemas
    {
      get => _applicableSchemas;
      set
      {
        this.RaiseAndSetIfChanged(ref _applicableSchemas, value);
        this.RaisePropertyChanged(nameof(Schemas));
        this.RaisePropertyChanged(nameof(ShowGeomMessage));
      }
    }


    private List<ISchema> _allSchemas = new List<ISchema>();

    public List<ISchema> AllSchemas
    {
      get => _allSchemas;
      set
      {
        this.RaiseAndSetIfChanged(ref _allSchemas, value);
        this.RaisePropertyChanged(nameof(Schemas));
        this.RaisePropertyChanged(nameof(ShowGeomMessage));
      }
    }


    public List<ISchema> Schemas
    {
      get
      {
        return AllSchemas.Where(x => ApplicableSchemas.Contains(x.GetType())).ToList();
      }
    }

    private ISchema _selectedSchema;

    public ISchema SelectedSchema
    {
      get => _selectedSchema;
      set => this.RaiseAndSetIfChanged(ref _selectedSchema, value);

    }

    public List<RevitElementType> AvailableRevitTypes { get; private set; } = new List<RevitElementType>();
    public List<string> AvailableRevitLevels { get; private set; } = new List<string>();



    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    internal static MappingsViewModel Instance { get; private set; }



    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
    }

    private bool _isValidStreamSelected = true;
    public bool IsValidStreamSelected
    {
      get => _isValidStreamSelected;
      private set => this.RaiseAndSetIfChanged(ref _isValidStreamSelected, value);
    }
    public bool ShowGeomMessage
    {
      get { return ApplicableSchemas.Count == 0 && AllSchemas.Any(); }
    }

    private StreamAccountWrapper _selectedStream;
    public StreamAccountWrapper SelectedStream
    {
      get => _selectedStream;
      private set => this.RaiseAndSetIfChanged(ref _selectedStream, value);
    }

    public StreamSelectorViewModel StreamSelector { get; private set; } = new StreamSelectorViewModel();





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
      RouterInstance = Router; // makes the router available app-wide

      RefreshSelectionCommand();

    }

    private void UpdateSelection(List<Type> types)
    {
      ApplicableSchemas = types;
    }

    internal async void OnBranchSelected()
    {
      try
      {

        var model = await GetCommit();

        if (model == null)
          return;

        GetTypesAndLevels(model);
        GenerateSchemas();

        SelectedStream = StreamSelector.SelectedStream;

        if (AvailableRevitTypes.Any() && AvailableRevitLevels.Any())
          IsValidStreamSelected = true;
        else
          IsValidStreamSelected = false;

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
        return;
      }


    }

    /// <summary>
    /// Manually patch info from our schema builder and the available model types
    /// </summary>
    /// <param name="revitTypes"></param>
    private void GenerateSchemas()
    {
      var schemas = new List<ISchema>();

      //WALLS
      var wallFamilies = AvailableRevitTypes.Where(x => x.category == "Walls").ToList();
      if (wallFamilies.Any())
      {
        var wallFamiliesViewModels = wallFamilies.GroupBy(x => x.family).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y.type).ToList())).ToList();
        schemas.Add(new RevitWallViewModel(wallFamiliesViewModels, AvailableRevitLevels));
      }

      //DIRECT SHAPE AND FREEFORM ELEMENT
      schemas.Add(new DirectShapeFreeformViewModel());

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
      AllSchemas = schemas;


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
      //needed bc we're serializing an interface
      var settings = new JsonSerializerSettings()
      {
        TypeNameHandling = TypeNameHandling.All
      };

      var serializedViewModel = JsonConvert.SerializeObject(SelectedSchema, settings);
      Bindings.SetMappings(SelectedSchema.GetSerializedSchema(), serializedViewModel);
    }

    public void RefreshSelectionCommand()
    {
      ApplicableSchemas = Bindings.GetSelectionSchemas();
    }

    public void OpenStreamSelectorCommand()
    {
      StreamSelector.IsVisible = true;
    }

  }


}

