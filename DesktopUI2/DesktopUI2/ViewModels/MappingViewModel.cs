using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels.Share;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows;
using DynamicData;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Stream = Speckle.Core.Api.Stream;

namespace DesktopUI2.ViewModels
{
  public class MappingViewModel : ReactiveObject, IRoutableViewModel
  {
    public string UrlPathSegment => throw new NotImplementedException();

    public IScreen HostScreen => throw new NotImplementedException();

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private string _searchQuery;
    public string SearchQuery 
    {
      get => _searchQuery;
      set => this.RaiseAndSetIfChanged(ref _searchQuery, value);
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
        Mapping.Where(i => i.IncomingType == CurrentTypeName).FirstOrDefault().OutgoingType = value;
        this.RaiseAndSetIfChanged(ref _selectedType, value);
      }
    }
    //private Dictionary<string, string> _mapping;
    //public Dictionary<string, string> Mapping 
    //{
    //  get => _mapping; 
    //  set => this.RaiseAndSetIfChanged(ref _mapping, value);
    //}
    //private Dictionary<string, string> _initialMapping;
    //public Dictionary<string, string> InitialMapping
    //{
    //  get => _initialMapping;
    //  set => this.RaiseAndSetIfChanged(ref _initialMapping, value);
    //}

    public ObservableCollection<MappingValue> Mapping { get; set; }
    public List<string> tabs { get; set; }

    private string _currentTypeName;
    public string CurrentTypeName
    {
      get => _currentTypeName;
      set => this.RaiseAndSetIfChanged(ref _currentTypeName, value);
    }
    public ICommand SetCurrentTypeName { get; set; }

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
      //InitialMapping = firstPassMapping;
      //Mapping = InitialMapping.ToDictionary(entry => entry.Key, entry => "");

      Mapping = new ObservableCollection<MappingValue>(firstPassMapping.Select(kvp => new MappingValue(kvp.Key, kvp.Value)).ToList());
      SearchResults = hostTypes;

      CurrentTypeName = "dummy CurrentTypeName";
      SetCurrentTypeName = ReactiveCommand.Create<object>(x => CurrentTypeName = (string)x);
    }

    public class MappingValue : ReactiveObject
    {
      public string IncomingType { get; set; }

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
  }
}
