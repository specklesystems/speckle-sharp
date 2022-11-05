using Objects.BuiltElements.Revit;
using Objects.Organization;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class MappingsViewModel : ViewModelBase, IScreen
  {
    public string TitleFull => "Speckle Mappings";
    public RoutingState Router { get; private set; }

    public MappingsBindings Bindings { get; private set; } = new DummyMappingsBindings();

    private List<RevitMappingViewModel> _revitMappings = new List<RevitMappingViewModel>();

    public List<RevitMappingViewModel> RevitMappings
    {
      get => _revitMappings;
      set => this.RaiseAndSetIfChanged(ref _revitMappings, value);
    }

    private RevitMappingViewModel _selectedRevitMapping;

    public RevitMappingViewModel SelectedRevitMapping
    {
      get => _selectedRevitMapping;
      set => this.RaiseAndSetIfChanged(ref _selectedRevitMapping, value);
    }

    private RevitElementType _selectedRevitType;

    public RevitElementType SelectedRevitType
    {
      get => _selectedRevitType;
      set => this.RaiseAndSetIfChanged(ref _selectedRevitType, value);
    }

    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    internal static MappingsViewModel Instance { get; private set; }
    public List<Base> Selection
    {
      get => _selection;
      set
      {
        _selection = value;
        this.RaiseAndSetIfChanged(ref _selection, value);
      }
    }

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

    private List<Base> _selection;

    public MappingsViewModel()
    {
      Instance = this;
      Router = new RoutingState();

      RouterInstance = Router; // makes the router available app-wide

      Selection = Bindings.GetSelection();

      StreamSelector.ObservableForProperty(x => x.SelectedBranch).Subscribe(x =>
      Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        Task.Run(() => GetCommit().ConfigureAwait(false))
      ));
    }

    private async Task GetCommit()
    {
      try
      {
        if (StreamSelector.SelectedBranch == null || StreamSelector.SelectedBranch.commits == null || StreamSelector.SelectedBranch.commits.items.Count == 0)
          return;

        ShowProgress = true;

        var client = new Client(StreamSelector.SelectedStream.Account);
        string referencedObject = StreamSelector.SelectedBranch.commits.items.FirstOrDefault().referencedObject;

        var transport = new ServerTransport(StreamSelector.SelectedStream.Account, StreamSelector.SelectedStream.Stream.id);
        var model = await Operations.Receive(
            referencedObject,
            transport,
            disposeTransports: true
            ) as Model;

        var types = model["Types"] as Base;

        var mappings = new List<RevitMappingViewModel>();

        foreach (var baseCategory in types.GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic))
        {
          try
          {
            var categoryName = baseCategory.Key.Replace("@", "");
            var elementTypes = (baseCategory.Value as List<object>).Cast<RevitElementType>().OrderBy(x => x.family + x.type).ToList();
            if (!elementTypes.Any())
              continue;

            mappings.Add(new RevitMappingViewModel
            {
              Category = categoryName,
              Types = elementTypes
            });

          }
          catch (Exception ex)
          {
            continue;
          }
        }

        RevitMappings = mappings;

        SelectedRevitMapping = RevitMappings[0];
        SelectedRevitType = SelectedRevitMapping.Types[0];
      }
      catch (Exception e)
      {

      }
      finally
      {
        ShowProgress = false;
      }
    }
  }
}

