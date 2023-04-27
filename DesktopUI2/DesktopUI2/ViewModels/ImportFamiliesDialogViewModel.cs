using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;

namespace DesktopUI2.ViewModels;

public class ImportFamiliesDialogViewModel : ReactiveObject
{
  private List<Symbol> _familyTypes;

  private bool _isTopBoxChecked;

  private string _searchQuery;

  private string _selectedFamily;

  public bool isSearching;

  public ImportFamiliesDialogViewModel(Dictionary<string, List<Symbol>> allSymbols)
  {
    Instance = this;
    this.allSymbols = allSymbols;

    foreach (var symbol in allSymbols.Keys)
      LoadedFamilies.Add(symbol);
    if (LoadedFamilies.Count > 0)
      SelectedFamily = LoadedFamilies[0];
  }

  // this constructor is only for xaml design purposes
  public ImportFamiliesDialogViewModel()
  {
    Instance = this;
    allSymbols = new Dictionary<string, List<Symbol>>
    {
      {
        "WF",
        new List<Symbol>
        {
          new("W12x19", "WF"),
          new("a very very ver long type name", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF"),
          new("W12x19", "WF")
        }
      },
      {
        "diff",
        new List<Symbol>
        {
          new("W12x21", "diff", true),
          new("W12x35", "diff", true),
          new("W12x40", "diff"),
          new("W12x19", "diff")
        }
      }
    };

    foreach (var symbol in allSymbols.Keys)
      LoadedFamilies.Add(symbol);
    if (LoadedFamilies.Count > 0)
      SelectedFamily = LoadedFamilies[0];
  }

  public static ImportFamiliesDialogViewModel Instance { get; private set; }

  public List<Symbol> FamilyTypes
  {
    get => _familyTypes;
    set => this.RaiseAndSetIfChanged(ref _familyTypes, value);
  }

  public Dictionary<string, List<Symbol>> allSymbols { get; private set; }
  public ObservableCollection<Symbol> selectedFamilySymbols { get; set; } = new();

  public string SearchQuery
  {
    get => _searchQuery;
    set
    {
      isSearching = true;
      this.RaiseAndSetIfChanged(ref _searchQuery, value);
      FamilyTypes = new List<Symbol>(
        allSymbols[SelectedFamily].Where(v => v.Name.ToLower().Contains(SearchQuery.ToLower())).ToList()
      );
      isSearching = false;
    }
  }

  public bool IsTopBoxChecked
  {
    get => _isTopBoxChecked;
    set
    {
      this.RaiseAndSetIfChanged(ref _isTopBoxChecked, value);
      if (value)
      {
        foreach (var type in FamilyTypes)
          if (!type.isChecked && !type.isImported)
            type.isChecked = true;
      }
      else
      {
        foreach (var type in FamilyTypes)
          type.isChecked = false;
      }
    }
  }

  public string SelectedFamily
  {
    get => _selectedFamily;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedFamily, value);
      FamilyTypes = allSymbols[value];
      SetTopBox();
    }
  }

  public ObservableCollection<string> LoadedFamilies { get; set; } = new();

  public event EventHandler OnRequestClose;

  public void ImportSymbolsCommand()
  {
    ImportFamiliesDialog.Instance.Close(null);
  }

  public void CloseDialogCommand()
  {
    selectedFamilySymbols = new ObservableCollection<Symbol>();
    ImportFamiliesDialog.Instance.Close(null);
  }

  public void ClearSearchCommand()
  {
    SearchQuery = "";
  }

  public void SetTopBox()
  {
    var allChecked = true;
    foreach (var type in FamilyTypes)
      if (!type.isChecked && !type.isImported)
      {
        allChecked = false;
        break;
      }

    _isTopBoxChecked = allChecked;
    this.RaisePropertyChanged(nameof(IsTopBoxChecked));
  }

  public class Symbol : ReactiveObject
  {
    private bool _isChecked;

    public Symbol(string name, string familyName, bool isImported = false)
    {
      Name = name;
      FamilyName = familyName;
      this.isImported = isImported;
    }

    public string Name { get; set; }
    public bool isImported { get; set; }
    public string FamilyName { get; set; }

    public bool isChecked
    {
      get => _isChecked;
      set
      {
        this.RaiseAndSetIfChanged(ref _isChecked, value);
        Instance.SetTopBox();
        if (value)
          Instance.selectedFamilySymbols.Add(this);
        else if (value == false && Instance.selectedFamilySymbols.Contains(this))
          Instance.selectedFamilySymbols.Remove(this);
      }
    }
  }
}
