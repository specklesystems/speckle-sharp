using Avalonia;
using DesktopUI2.Views.Windows.Dialogs;
using Newtonsoft.Json;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DesktopUI2.ViewModels
{
  public class MappingViewModel : ReactiveObject, IRoutableViewModel
  {
    public string UrlPathSegment => throw new NotImplementedException();

    public IScreen HostScreen => throw new NotImplementedException();

    //public event EventHandler OnRequestClose;

    public ConnectorBindings Bindings { get; set; }

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    public const string UnmappedKey = "New Incoming Types";
    private string TypeCatMisc = "Miscellaneous"; // needs to match string in connectorRevit.Mappings

    private bool isSearching = false;
    private Dictionary<string,List<string>> _hostTypeValuesDict { get; } = new Dictionary<string, List<string>>();
    private string _searchQuery;
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = new List<string>(_hostTypeValuesDict[SelectedCategory].Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
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

    private string _selectedType;
    public string SelectedType
    {
      get => _selectedType;
      set
      {
        SelectedMappingValue.OutgoingType = value;
        this.RaiseAndSetIfChanged(ref _selectedType, value);
      }
    }


    public Dictionary<string,List<MappingValue>> Mapping { get; set; }

    private string _selectedCategory;
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
          {
            tempList.AddRange(Mapping[key].Where(i => i.NewType == true));
          }
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

    private List<MappingValue> _visibleMappingValues;
    public List<MappingValue> VisibleMappingValues
    {
      get => _visibleMappingValues;
      set => this.RaiseAndSetIfChanged(ref _visibleMappingValues, value);
    }

    private MappingValue _selectedMappingValue;
    public MappingValue SelectedMappingValue
    {
      get => _selectedMappingValue;
      set => this.RaiseAndSetIfChanged(ref _selectedMappingValue, value);
    }

    //this constructor is purely for xaml design purposes
    public MappingViewModel()
    {

      Mapping = new Dictionary<string, List<MappingValue>>
      {
        {
          "Materials", new List<MappingValue>
          (
            new List<MappingValue>
            {
              new MappingValue("W12x19", "W12x19"),
              new MappingValue("Type1", "type123"),
              new MappingValue("anotherType", "anotherType"),
              new MappingValue("yetAnotherType", "differentType", true),
              new MappingValue("short", "short"),
              new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no")
            }
          )
        },
        {
          "Beams", new List<MappingValue>
          (
            new List<MappingValue>
            {
              new MappingValue("W12x19", "W12x19"),
              new MappingValue("Wood Beam", "type123"),
              new MappingValue("Glulam", "anotherType", true),
              new MappingValue("Conc Beam", "differentType" ),
              new MappingValue("short", "short"),
              new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no")
            }
          )
        },
        {
          "Columns", new List<MappingValue>
          (
            new List<MappingValue>
            {
              new MappingValue("W12x19", "W12x19"),
              new MappingValue("col", "type123", true),
            }
          )
        },
      };

      Mapping[UnmappedKey] = new List<MappingValue>();

      _hostTypeValuesDict = new Dictionary<string, List<string>>
      {
        { "Materials", new List<string>{
          "brick","sheep","wheat","stone","brick","sheep","wheat","stone","brick","sheep","wheat","stone",
          "brick","sheep","wheat","stone","brick","sheep","wheat","stone","brick","sheep","wheat","stone",
          "brick","sheep","wheat","stone","brick","sheep","wheat","stone","brick","sheep","wheat","stone",
          "brick","sheep","wheat","stone","brick","sheep","wheat","stone","brick","sheep","wheat","stone",
          "brick","sheep","wheat","stone","brick","sheep","wheat","stone","brick","sheep","wheat","stone",
        } },
        { "Beams", new List<string>{"concrete","tile"} },
        { "Columns", new List<string>{"brick","gyp","shearwall1" } }
      };

      var kv = Mapping.First();
      SelectedCategory = kv.Key;
    }


    public MappingViewModel(Dictionary<string, List<MappingValue>> firstPassMapping, Dictionary<string, List<string>> hostTypesDict, ProgressViewModel progress, bool newTypesExist = false)
    {
      Mapping = new Dictionary<string, List<MappingValue>>();
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      Mapping = firstPassMapping;
      _hostTypeValuesDict = hostTypesDict;

      // make sure hostTypeValuesDict has a key for each value category
      foreach (var key in Mapping.Keys)
      {
        if (!_hostTypeValuesDict.ContainsKey(key))
        {
          _hostTypeValuesDict.Add(key, _hostTypeValuesDict[TypeCatMisc]);
        }
      }

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

    }

    [DataContract]
    public class MappingValue : ReactiveObject
    {
      [DataMember]
      public string IncomingType { get; set; }
      public bool Imported { get; set; }
      public bool NewType { get; set; }

      private string _initialGuess;
      [DataMember]
      public string InitialGuess
      {
        get => _initialGuess;
        set => this.RaiseAndSetIfChanged(ref _initialGuess, value);
      }
      private string _outgoingType;
      [DataMember]
      public string OutgoingType
      {
        get => _outgoingType;
        set => this.RaiseAndSetIfChanged(ref _outgoingType, value);
      }
      private string _outgoingFamily;
      public string OutgoingFamily
      {
        get => _outgoingFamily;
        set => this.RaiseAndSetIfChanged(ref _outgoingFamily, value);
      }
      public MappingValue(string inType, string inGuess, bool inNewType = false)
      {
        IncomingType = inType;
        InitialGuess = inGuess;
        NewType = inNewType;
      }
    }

    public async void ImportFamily()
    {
      Mapping = await Bindings.ImportFamily(Mapping);
    }

    public void Done()
    {
      MappingViewDialog.Instance.Close(Mapping);
    }
  }
}
