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

    public List<Base> AvailableRevitTypes { get; private set; } = new List<Base>();
    public List<string> AvailableRevitLevels { get; private set; } = new List<string>();



    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    internal static MappingsViewModel Instance { get; private set; }



    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showProgress, value);
      }
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

    internal async Task OnBranchSelected()
    {
      try
      {

        var model = await GetCommit();

        if (model == null)
          return;

        GetAvailableRevitMetadata(model);
        GenerateRevitMetadata();

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

    private void GetAvailableRevitMetadata(Base model)
    {
      var revitTypes = new List<Base>();
      try
      {
        var types = model["Types"] as Base;

        foreach (var baseCategory in types.GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic))
        {
          try
          {
            var elementTypes = (baseCategory.Value as List<object>).Cast<Base>().ToList();
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
        AvailableRevitLevels = (model["@Levels"] as List<object>).Cast<Base>().Select(x => x["name"].ToString()).ToList();

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
    private void GenerateRevitMetadata()
    {
      var revitViewModels = new List<ISchema>();

      //WALLS
      var wallFamilies = AvailableRevitTypes.Where(x => x["category"].ToString() == "Walls").ToList();
      if (wallFamilies.Any())
      {
        var wallFamiliesViewModels = wallFamilies.GroupBy(x => x["family"]).Select(g => new RevitFamily(g.Key.ToString(), g.Select(y => y["type"].ToString()).ToList())).ToList();
        revitViewModels.Add(new RevitWallViewModel(wallFamiliesViewModels, AvailableRevitLevels));
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

      ////WALLS
      //var wallFamilies = revitTypes.Where(x => x.category == "Walls").ToList();
      //if (wallFamilies.Any())
      //{
      //  revitMetadata.Add(new RevitMetadataViewModel("Wall", new List<Type> { typeof(RevitWall) }, wallFamilies));
      //}

      //also triggers binding refresh
      AllSchemas = revitViewModels;


    }


    public void SetMappingsCommand()
    {
      //needed bc we're serializing an interface
      var settings = new JsonSerializerSettings()
      {
        TypeNameHandling = TypeNameHandling.All
      };

      var serialized = JsonConvert.SerializeObject(SelectedSchema, settings);

      Bindings.SetMappings(serialized);
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

