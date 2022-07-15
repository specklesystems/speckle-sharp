using Avalonia;
using Newtonsoft.Json;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Xml.Serialization;

namespace DesktopUI2.ViewModels
{
  public class MappingViewModel : ReactiveObject, IRoutableViewModel
  {
    public string UrlPathSegment => throw new NotImplementedException();

    public IScreen HostScreen => throw new NotImplementedException();

    public event EventHandler OnRequestClose;

    public ConnectorBindings Bindings { get; set; }

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private bool isSearching = false;
    private List<string> _valuesList { get; } = new List<string>();
    private string _searchQuery;
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = new List<string>(_valuesList.Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
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

    public ObservableCollection<MappingValue> Mapping { get; set; }

    private MappingValue _selectedMappingValue;
    public MappingValue SelectedMappingValue
    {
      get => _selectedMappingValue;
      set => this.RaiseAndSetIfChanged(ref _selectedMappingValue, value);
    }
    public List<string> tabs { get; set; }

    public MappingViewModel()
    {
    //  var x = new List<MappingValue>
    //  {
    //    new MappingValue("W12x19", "W12x19"),
    //    new MappingValue("Type1", "type123"),
    //    new MappingValue("anotherType", "anotherType"),
    //    new MappingValue("yetAnotherType", "differentType" ),
    //    new MappingValue("short", "short"),
    //    new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no");
    //  }
    Mapping = new ObservableCollection<MappingValue>
      (new List<MappingValue>
        {
          new MappingValue("W12x19", "W12x19"),
          new MappingValue("Type1", "type123"),
          new MappingValue("anotherType", "anotherType"),
          new MappingValue("yetAnotherType", "differentType" ),
          new MappingValue("short", "short"),
          new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no")
        }
      );
    }

    public MappingViewModel(Dictionary<string, string> firstPassMapping, List<string> hostTypes)
    {
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      Mapping = new ObservableCollection<MappingValue>(firstPassMapping.Select(kvp => new MappingValue(kvp.Key, kvp.Value)).ToList());
      _valuesList = SearchResults = hostTypes;
    }

    public class MappingValue : ReactiveObject
    {
      public string IncomingType { get; set; }
      public bool Imported { get; set; }

      private string _initialGuess;
      public string InitialGuess
      {
        get => _initialGuess;
        set => this.RaiseAndSetIfChanged(ref _initialGuess, value);
      }
      private string _outgoingType;
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

    public async void ImportFamily()
    {
      Mapping = await Bindings.ImportFamily(Mapping);
    }

    public void Done()
    {
      Dictionary<string, string> mappingDict = Mapping.ToDictionary(x => x.IncomingType, x => x.OutgoingType ?? x.InitialGuess);
      var json = JsonConvert.SerializeObject(mappingDict);
      Bindings.MappingSelectionValue = json;
      OnRequestClose(this, new EventArgs());
    }
  }
}
