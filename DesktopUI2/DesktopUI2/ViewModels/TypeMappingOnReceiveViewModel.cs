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

    private List<string> _searchResults;

    private string _selectedCategory;

    private ISingleValueToMap _selectedMappingValue;

    private string _selectedType;

    private List<ISingleValueToMap> _visibleMappingValues;
    public bool DoneMapping;

    public const string TypeCatMisc = "Miscellaneous";

    //this constructor is purely for xaml design purposes
    public TypeMappingOnReceiveViewModel()
    {
      VisibleMappingValues = new List<ISingleValueToMap>()
      {
        new MappingValue("W12x19", "W12x19"),
        new MappingValue("Type1", "type123"),
        new MappingValue("anotherType", "anotherType"),
        new MappingValue("incoming type", "existingType", "incomingFam"),
        new MappingValue("yetAnotherType", "differentType", "inFam", true),
        new MappingValue("short", "short"),
        new MappingValue("a very very very long type name. Oh no", "a very very very long type name. Oh no"),
        new MappingValue("W12x19", "W12x19"),
        new MappingValue("Type1", "type123"),
        new MappingValue("anotherType", "anotherType"),
        new MappingValue("incoming type", "existingType", "incomingFam"),
        new MappingValue("yetAnotherType", "differentType", "inFam", true),
        new MappingValue("short", "short"),
        new MappingValue("a very very very long type name. Oh no", "a very very very long type name. Oh no"),
        new MappingValue("W12x19", "W12x19"),
        new MappingValue("Type1", "type123"),
        new MappingValue("anotherType", "anotherType"),
        new MappingValue("incoming type", "existingType", "incomingFam"),
        new MappingValue("yetAnotherType", "differentType", "inFam", true),
        new MappingValue("short", "short"),
        new MappingValue("a very very very long type name. Oh no", "a very very very long type name. Oh no"),
        new MappingValue("W12x19", "W12x19"),
        new MappingValue("Type1", "type123"),
        new MappingValue("anotherType", "anotherType"),
        new MappingValue("incoming type", "existingType", "incomingFam"),
        new MappingValue("yetAnotherType", "differentType", "inFam", true),
        new MappingValue("short", "short"),
        new MappingValue("a very very very long type name. Oh no", "a very very very long type name. Oh no"),
      };

      SearchResults = new List<string>
      {
        "brick",
        "sheep",
        "wheat",
        "stone",
      };
    }

    public TypeMappingOnReceiveViewModel(
      ITypeMap typeMap,
      IHostTypeAsStringContainer container,
      bool newTypesExist = false
    )
    {
      Mapping = typeMap;
      hostTypeContainer = container;
      SelectedCategory = Mapping.Categories.First();

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Mappings Open" } });
    }

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private readonly IHostTypeAsStringContainer hostTypeContainer;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = GetCategoryOrAll(SelectedCategory).Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList();
        this.RaisePropertyChanged(nameof(SearchResults));
      }
    }

    public List<string> SearchResults
    {
      get => _searchResults;
      set => this.RaiseAndSetIfChanged(ref _searchResults, value);
    }

    public string SelectedType
    {
      get => _selectedType;
      set
      {
        if (SelectedMappingValues.Count > 0)
        {
          foreach (var val in SelectedMappingValues)
          {
            val.OutgoingType = value;
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
      }
    }

    public List<ISingleValueToMap> VisibleMappingValues
    {
      get => _visibleMappingValues;
      set => this.RaiseAndSetIfChanged(ref _visibleMappingValues, value);
    }

    public ObservableCollection<ISingleValueToMap> SelectedMappingValues { get; } = new();

    private IEnumerable<string> GetCategoryOrAll(string category)
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
