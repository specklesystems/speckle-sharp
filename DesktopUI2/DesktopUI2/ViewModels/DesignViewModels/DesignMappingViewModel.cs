using ReactiveUI;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignMappingViewModel : ReactiveObject
  {
    public string Title => "for Revit";
    public string TitleFull => "Scheduler for Revit";
    public string Test { get; set; }
    private Dictionary<string, List<string>> _valuesList { get; } = new Dictionary<string, List<string>>();
    public Dictionary<string,ObservableCollection<MappingValue>> Mapping { get; set; }
    public ObservableCollection<MappingValue> visibleMappingValues { get; set; }
    private bool isSearching = false;
    private string _searchQuery;
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = new List<string>(_valuesList[SelectedCategory].Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
        this.RaisePropertyChanged(nameof(SearchResults));
        isSearching = false;
      }
    }

    private List<string> _searchResults;
    public List<string> SearchResults
    {
      get => _searchResults;
      set => this.RaiseAndSetIfChanged(ref _searchResults, value);
    }

    private string _selectedCategory;
    public string SelectedCategory
    {
      get => _selectedCategory;
      set
      {
        VisibleMappingValues = new List<MappingValue>(Mapping[value]);
        this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        SearchQuery = "";
        SearchResults = _valuesList[value];
      }
    }

    private string _selectedType;
    public string SelectedType
    {
      get => _selectedType;
      set
      {
        SelectedMappingValue.OutgoingType = value;
        //this.RaisePropertyChanged("VisibleMappingValues");
        this.RaiseAndSetIfChanged(ref _selectedType, value);
      }
    }

    private MappingValue _selectedMappingValue;
    public MappingValue SelectedMappingValue
    {
      get => _selectedMappingValue;
      set => this.RaiseAndSetIfChanged(ref _selectedMappingValue, value);
    }

    private List<MappingValue> _visibleMappingValues;
    public List<MappingValue> VisibleMappingValues
    {
      get => _visibleMappingValues;
      set => this.RaiseAndSetIfChanged(ref _visibleMappingValues, value);
    }

    public string CurrentTypeName { get; set; } = "Testing 123";

    public DesignMappingViewModel()
    {

      Mapping = new Dictionary<string, ObservableCollection<MappingValue>>
      {
        {
          "Materials", new ObservableCollection<MappingValue>
          (
            new List<MappingValue>
            {
              new MappingValue("W12x19", "W12x19"),
              new MappingValue("Type1", "type123"),
              new MappingValue("anotherType", "anotherType"),
              new MappingValue("yetAnotherType", "differentType" ),
              new MappingValue("short", "short"),
              new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no")
            }
          )
        },
        {
          "Beams", new ObservableCollection<MappingValue>
          (
            new List<MappingValue>
            {
              new MappingValue("W12x19", "W12x19"),
              new MappingValue("Wood Beam", "type123"),
              new MappingValue("Glulam", "anotherType"),
              new MappingValue("Conc Beam", "differentType" ),
              new MappingValue("short", "short"),
              new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no")
            }
          )
        },
        {
          "Columns", new ObservableCollection<MappingValue>
          (
            new List<MappingValue>
            {
              new MappingValue("W12x19", "W12x19"),
              new MappingValue("col", "type123"),
            }
          )
        },
      };

      _valuesList = new Dictionary<string, List<string>>
      {
        { "Materials", new List<string>{"brick","sheep","wheat","stone" } },
        { "Beams", new List<string>{"concrete","tile"} },
        { "Columns", new List<string>{"brick","gyp","shearwall1" } }
      };

      var kv = Mapping.First();
      SelectedCategory = kv.Key;
      //visibleMappingValues = Mapping[SelectedCategory];

      //SearchResults = new List<string>
      //{
      //  "Type1",
      //  "Type2",
      //  "Type3",
      //  "A wiiiiiide flange",
      //  "some wall maybe?",
      //  "roof",
      //  "other",
      //  "types",
      //  "are",
      //  "welcome"
      //};
      Test = "testing. Is this hooked up?";
    }
  }
}
