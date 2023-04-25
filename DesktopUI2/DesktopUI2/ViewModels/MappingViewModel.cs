using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.Serialization;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;

namespace DesktopUI2.ViewModels;

public class MappingViewModel : ReactiveObject, IRoutableViewModel
{
  public const string UnmappedKey = "New Incoming Types";
  private string _searchQuery;

  private List<string> _searchResults;

  private string _selectedCategory;

  private MappingValue _selectedMappingValue;

  private string _selectedType;

  private List<MappingValue> _visibleMappingValues;
  public bool DoneMapping;

  private bool isSearching;
  private string TypeCatMisc = "Miscellaneous"; // needs to match string in connectorRevit.Mappings

  //this constructor is purely for xaml design purposes
  public MappingViewModel()
  {
    Mapping = new Dictionary<string, List<MappingValue>>
    {
      {
        "Materials",
        new List<MappingValue>(
          new List<MappingValue>
          {
            new("W12x19", "W12x19"),
            new("Type1", "type123"),
            new("anotherType", "anotherType"),
            new("yetAnotherType", "differentType", true),
            new("short", "short"),
            new("a very very very long type name. Oh no", "a very very very long type name. Oh no")
          }
        )
      },
      {
        "Beams",
        new List<MappingValue>(
          new List<MappingValue>
          {
            new("W12x19", "W12x19"),
            new("Wood Beam", "type123"),
            new("Glulam", "anotherType", true),
            new("Conc Beam", "differentType"),
            new("short", "short"),
            new("a very very very long type name. Oh no", "a very very very long type name. Oh no")
          }
        )
      },
      {
        "Columns",
        new List<MappingValue>(new List<MappingValue> { new("W12x19", "W12x19"), new("col", "type123", true) })
      }
    };

    Mapping[UnmappedKey] = new List<MappingValue>();

    _hostTypeValuesDict = new Dictionary<string, List<string>>
    {
      {
        "Materials",
        new List<string>
        {
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone",
          "brick",
          "sheep",
          "wheat",
          "stone"
        }
      },
      {
        "Beams",
        new List<string> { "concrete", "tile" }
      },
      {
        "Columns",
        new List<string> { "brick", "gyp", "shearwall1" }
      }
    };

    var kv = Mapping.First();
    SelectedCategory = kv.Key;
  }

  public MappingViewModel(
    Dictionary<string, List<MappingValue>> firstPassMapping,
    Dictionary<string, List<string>> hostTypesDict,
    bool newTypesExist = false
  )
  {
    Mapping = new Dictionary<string, List<MappingValue>>();
    Bindings = Locator.Current.GetService<ConnectorBindings>();

    Mapping = firstPassMapping;
    _hostTypeValuesDict = hostTypesDict;

    // make sure hostTypeValuesDict has a key for each value category
    foreach (var key in Mapping.Keys)
      if (!_hostTypeValuesDict.ContainsKey(key))
        _hostTypeValuesDict.Add(key, _hostTypeValuesDict[TypeCatMisc]);

    if (newTypesExist)
    {
      // add key so it will show up in categories list
      Mapping[UnmappedKey] = new List<MappingValue>();
      _hostTypeValuesDict[UnmappedKey] = _hostTypeValuesDict[TypeCatMisc];
    }

    if (Mapping.ContainsKey(UnmappedKey))
      SelectedCategory = UnmappedKey;
    else
      SelectedCategory = Mapping.Keys.First();

    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Mappings Open" } });
  }

  public ConnectorBindings Bindings { get; set; }

  public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;
  private Dictionary<string, List<string>> _hostTypeValuesDict { get; } = new();

  public string SearchQuery
  {
    get => _searchQuery;
    set
    {
      isSearching = true;
      this.RaiseAndSetIfChanged(ref _searchQuery, value);

      SearchResults = new List<string>(
        _hostTypeValuesDict[SelectedCategory].Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList()
      );
      this.RaisePropertyChanged(nameof(SearchResults));
      isSearching = false;
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
      if (SelectedMappingValue != null)
        SelectedMappingValue.OutgoingType = value;
      this.RaiseAndSetIfChanged(ref _selectedType, value);
    }
  }

  public Dictionary<string, List<MappingValue>> Mapping { get; set; }

  public string SelectedCategory
  {
    get => _selectedCategory;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedCategory, value);
      if (value == UnmappedKey)
      {
        var tempList = new List<MappingValue>();
        foreach (var key in Mapping.Keys)
          tempList.AddRange(Mapping[key].Where(i => i.NewType));
        VisibleMappingValues = tempList;
      }
      else
      {
        VisibleMappingValues = new List<MappingValue>(Mapping[value]);
      }
      SearchQuery = "";
      SearchResults = _hostTypeValuesDict[value];
    }
  }

  public List<MappingValue> VisibleMappingValues
  {
    get => _visibleMappingValues;
    set => this.RaiseAndSetIfChanged(ref _visibleMappingValues, value);
  }

  public MappingValue SelectedMappingValue
  {
    get => _selectedMappingValue;
    set => this.RaiseAndSetIfChanged(ref _selectedMappingValue, value);
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

  [DataContract]
  public class MappingValue : ReactiveObject
  {
    private string _initialGuess;
    private string _outgoingFamily;
    private string _outgoingType;

    public MappingValue(string inType, string inGuess, bool inNewType = false)
    {
      IncomingType = inType;
      InitialGuess = inGuess;
      NewType = inNewType;
    }

    [DataMember]
    public string IncomingType { get; set; }

    public bool Imported { get; set; }
    public bool NewType { get; set; }

    [DataMember]
    public string InitialGuess
    {
      get => _initialGuess;
      set => this.RaiseAndSetIfChanged(ref _initialGuess, value);
    }

    [DataMember]
    public string OutgoingType
    {
      get => _outgoingType;
      set => this.RaiseAndSetIfChanged(ref _outgoingType, value);
    }

    public string OutgoingFamily
    {
      get => _outgoingFamily;
      set => this.RaiseAndSetIfChanged(ref _outgoingFamily, value);
    }
  }
}
