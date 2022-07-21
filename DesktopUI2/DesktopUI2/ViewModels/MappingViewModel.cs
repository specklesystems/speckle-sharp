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

    private bool isSearching = false;
    private Dictionary<string,List<string>> _valuesList { get; } = new Dictionary<string, List<string>>();
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
        VisibleMappingValues = new List<MappingValue>(Mapping[value]);
        this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        SearchQuery = "";
        SearchResults = _valuesList[value];
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
              new MappingValue("yetAnotherType", "differentType" ),
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
              new MappingValue("Glulam", "anotherType"),
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
    }


    public MappingViewModel(Dictionary<string, List<MappingValue>> firstPassMapping, Dictionary<string, List<string>> hostTypesDict, ProgressViewModel progress)
    {
      progress.Report.Log($"host type Dict keys {String.Join(",", hostTypesDict.Keys)}");
      progress.Report.Log($"host type Dict values {String.Join(",", hostTypesDict.Values)}");

      progress.Report.Log($"firstpass keys {String.Join(",", firstPassMapping.Keys)}");
      progress.Report.Log($"firstpass values {String.Join(",", firstPassMapping.Values)}");

      Mapping = new Dictionary<string, List<MappingValue>>();
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      //foreach (var category in firstPassMapping.Keys)
      //{
      //  Mapping[category] = new List<MappingValue>(firstPassMapping[category]);
      //}

      Mapping = firstPassMapping;

      //foreach (var category in firstPassMapping.Keys)
      //{
      //  progress.Report.Log($"category");
      //  foreach (var kvp in firstPassMapping[category])
      //  {
      //    if (Mapping.ContainsKey(category))
      //    {
      //      Mapping[category].Add(new MappingValue(kvp.InitialGuess, kvp.Value));
      //    }
      //    else
      //    {
      //      Mapping[category] = new List<MappingValue> { new MappingValue(kvp.InitialGuess, kvp.Value) };
      //    }
      //  }
      //}

      progress.Report.Log($"Mapping {Mapping}");
      _valuesList = hostTypesDict;

      SelectedCategory = Mapping.Keys.First();
      progress.Report.Log($"SelectedCategory {SelectedCategory}");
      VisibleMappingValues = new List<MappingValue>(Mapping[Mapping.Keys.First()]);
      progress.Report.Log($"visibleMappingValues {VisibleMappingValues}");
      //Mapping = new Dictionary<string,List<MappingValue>>(firstPassMapping.Select(kvp => new MappingValue(kvp.Key, kvp.Value)).ToList());
      //_valuesList = hostTypesDict;
      SearchResults = _valuesList[SelectedCategory];
      progress.Report.Log($"SearchResults {SearchResults}");
    }

    [DataContract]
    public class MappingValue : ReactiveObject
    {
      [DataMember]
      public string IncomingType { get; set; }
      public bool Imported { get; set; }

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
      public MappingValue(string inType, string inGuess)
      {
        IncomingType = inType;
        InitialGuess = inGuess;
      }
    }

    //public class MappingValue : ReactiveObject
    //{
    //  public string IncomingType { get; set; }
    //  public bool Imported { get; set; }

    //  private string _initialGuess;
    //  public string InitialGuess
    //  {
    //    get => _initialGuess;
    //    set => this.RaiseAndSetIfChanged(ref _initialGuess, value);
    //  }
    //  private string _outgoingType;
    //  public string OutgoingType
    //  {
    //    get => _outgoingType;
    //    set => this.RaiseAndSetIfChanged(ref _outgoingType, value);
    //  }
    //  private string _outgoingFamily;
    //  public string OutgoingFamily
    //  {
    //    get => _outgoingFamily;
    //    set => this.RaiseAndSetIfChanged(ref _outgoingFamily, value);
    //  }
    //  public MappingValue(string inType, string inGuess)
    //  {
    //    IncomingType = inType;
    //    InitialGuess = inGuess;
    //  }
    //}

    public async void ImportFamily()
    {
      Mapping = await Bindings.ImportFamily(Mapping);
    }

    public void Done()
    {
      //Dictionary<string, List<MappingValue>> mappingDict = new Dictionary<string, List<MappingValue>>();
      //foreach (var category in Mapping.Keys)
      //{
      //  foreach (var mappingValue in Mapping[category])
      //  {
      //    if (mappingDict.ContainsKey(category))
      //    {
      //      mappingDict[category].Add(new MappingValue(mappingValue.IncomingType, mappingValue.OutgoingType ?? mappingValue.InitialGuess));
      //    }
      //    else
      //    {
      //      mappingDict[category] = new List<MappingValue>
      //      {
      //        new MappingValue
      //        (
      //          mappingValue.IncomingType, mappingValue.OutgoingType ?? mappingValue.InitialGuess
      //        )
      //      };
      //    }
      //  }
      //}
      //var json = JsonConvert.SerializeObject(Mapping);
      //Bindings.MappingSelectionValue = json;

      MappingViewDialog.Instance.Close(Mapping);
      //MappingViewDialog.Instance.Close(Mapping);

      //OnRequestClose(this, new EventArgs());
      //return mappingDict;
      //Dictionary<string, string> mappingDict = Mapping.ToDictionary(x => x.IncomingType, x => x.OutgoingType ?? x.InitialGuess);
      //var json = JsonConvert.SerializeObject(mappingDict);
      //Bindings.MappingSelectionValue = json;

      //OnRequestClose(this, new EventArgs());
    }
  }
}
