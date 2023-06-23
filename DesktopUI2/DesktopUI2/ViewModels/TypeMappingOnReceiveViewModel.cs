using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Logging;

namespace DesktopUI2.ViewModels
{
  public class TypeMappingOnReceiveViewModel : ReactiveObject, IRoutableViewModel
  {
    public const string UnmappedKey = "New Incoming Types";
    private string _searchQuery;

    private string _selectedCategory;

    private ISingleValueToMap _selectedMappingValue;

    private List<ISingleValueToMap> _visibleMappingValues;
    public bool DoneMapping;

    public const string TypeCatMisc = "Miscellaneous";

    public TypeMappingOnReceiveViewModel(
      ITypeMap typeMap,
      IHostTypeContainer container,
      bool newTypesExist = false
    )
    {
      Mapping = typeMap;
      hostTypeContainer = container;
      SelectedCategory = Mapping.Categories.First();

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Mappings Open" } });
    }

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private readonly IHostTypeContainer hostTypeContainer;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = GetCategoryOrAll(SelectedCategory).Where(ht => ht.HostTypeDisplayName.ToLower().Contains(SearchQuery.ToLower())).ToList();
        this.RaisePropertyChanged(nameof(SearchResults));
      }
    }

    private ISingleHostType _selectedType;
    public ISingleHostType SelectedType
    {
      get => _selectedType;
      set
      {
        if (SelectedMappingValues.Count > 0)
        {
          foreach (var val in SelectedMappingValues)
          {
            val.MappedHostType = value;
          }
        }
        this.RaiseAndSetIfChanged(ref _selectedType, value);
      }
    }

    public ITypeMap Mapping { get; set; }

    public string SelectedCategory
    {
      get => _selectedCategory;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        VisibleMappingValues = Mapping.GetValuesToMapOfCategory(value).ToList();
        SearchQuery = "";
        SearchResults = GetCategoryOrAll(value).ToList();
        SelectedMappingValues.Clear();
      }
    }

    public List<ISingleValueToMap> VisibleMappingValues
    {
      get => _visibleMappingValues;
      set => this.RaiseAndSetIfChanged(ref _visibleMappingValues, value);
    }

    public ObservableCollection<ISingleValueToMap> SelectedMappingValues { get; } = new();

    private List<ISingleHostType> _searchResults;
    public List<ISingleHostType> SearchResults
    {
      get => _searchResults;
      set => this.RaiseAndSetIfChanged(ref _searchResults, value);
    }

    private ICollection<ISingleHostType> GetCategoryOrAll(string category)
    {
      return hostTypeContainer.GetTypesInCategory(category) ?? hostTypeContainer.GetAllTypes();
    }

    public string UrlPathSegment => throw new NotImplementedException();

    public IScreen HostScreen => throw new NotImplementedException();

    public void ImportFamilyCommand()
    {
      MappingViewDialog.Instance.Close(Mapping);
    }

    public void Done()
    {
      DoneMapping = true;
      MappingViewDialog.Instance.Close(Mapping);
    }
  }
}
