using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.Serialization;
using System.Text;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json.Linq;
using Splat;

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

    private bool isSearching;
    public const string TypeCatMisc = "Miscellaneous"; // needs to match string in connectorRevit.Mappings

    //this constructor is purely for xaml design purposes
    //public TypeMappingOnReceiveViewModel()
    //{
    //  Mapping = new Dictionary<string, List<MappingValue>>
    //{
    //  {
    //    "Materials",
    //    new List<MappingValue>(
    //      new List<MappingValue>
    //      {
    //        new("W12x19", "W12x19"),
    //        new("Type1", "type123"),
    //        new("anotherType", "anotherType"),
    //        new("yetAnotherType", "differentType", true),
    //        new("short", "short"),
    //        new("a very very very long type name. Oh no", "a very very very long type name. Oh no")
    //      }
    //    )
    //  },
    //  {
    //    "Beams",
    //    new List<MappingValue>(
    //      new List<MappingValue>
    //      {
    //        new("W12x19", "W12x19"),
    //        new("Wood Beam", "type123"),
    //        new("Glulam", "anotherType", true),
    //        new("Conc Beam", "differentType"),
    //        new("short", "short"),
    //        new("a very very very long type name. Oh no", "a very very very long type name. Oh no")
    //      }
    //    )
    //  },
    //  {
    //    "Columns",
    //    new List<MappingValue>(new List<MappingValue> { new("W12x19", "W12x19"), new("col", "type123", true) })
    //  }
    //};

    //  Mapping[UnmappedKey] = new List<MappingValue>();

    //  _hostTypeValuesDict = new Dictionary<string, List<string>>
    //{
    //  {
    //    "Materials",
    //    new List<string>
    //    {
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone",
    //      "brick",
    //      "sheep",
    //      "wheat",
    //      "stone"
    //    }
    //  },
    //  {
    //    "Beams",
    //    new List<string> { "concrete", "tile" }
    //  },
    //  {
    //    "Columns",
    //    new List<string> { "brick", "gyp", "shearwall1" }
    //  }
    //};

    //  var kv = Mapping.First();
    //  SelectedCategory = kv.Key;
    //}

    public TypeMappingOnReceiveViewModel(
      //Dictionary<string, List<MappingValue>> firstPassMapping,
      ITypeMap typeMap,
      //Dictionary<string, List<string>> hostTypesDict,
      IHostTypeAsStringContainer container,
      bool newTypesExist = false
    )
    {
      //Bindings = Locator.Current.GetService<ConnectorBindings>();

      Mapping = typeMap;
      //_hostTypeValuesDict = hostTypesDict;
      hostTypeContainer = container;

      //if (!hostTypeContainer.ContainsCategory(TypeCatMisc))
      //{
      //  throw new ArgumentException("Provided host type map does not contain a miscellaneous key with all element types");
      //}

      //// make sure hostTypeValuesDict has a key for each value category
      //foreach (var key in Mapping.Categories)
      //  hostTypeContainer.AddCategoryWithTypesIfCategoryIsNew(key, hostTypeContainer.GetAllTypes());
      //  if (!hostTypeContainer.ContainsCategory(key))
      //    _hostTypeValuesDict.Add(key, _hostTypeValuesDict[TypeCatMisc]);

      //if (newTypesExist)
      //{
      //  // add key so it will show up in categories list
      //  Mapping[UnmappedKey] = new List<MappingValue>();
      //  _hostTypeValuesDict[UnmappedKey] = _hostTypeValuesDict[TypeCatMisc];
      //}

      //if (Mapping.HasCategory(UnmappedKey))
      //  SelectedCategory = UnmappedKey;
      //else
      //  SelectedCategory = Mapping.Categories.First();
      SelectedCategory = Mapping.Categories.First();

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Mappings Open" } });
    }

    //public ConnectorBindings Bindings { get; set; }

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;
    //private Dictionary<string, List<string>> _hostTypeValuesDict { get; } = new();

    private readonly IHostTypeAsStringContainer hostTypeContainer;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = GetCategoryOrAll(SelectedCategory).Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList();
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

    public ITypeMap Mapping { get; set; }

    public string SelectedCategory
    {
      get => _selectedCategory;
      set
      {
        _selectedCategory = value;
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

    public ISingleValueToMap SelectedMappingValue
    {
      get => _selectedMappingValue;
      set => this.RaiseAndSetIfChanged(ref _selectedMappingValue, value);
    }

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
