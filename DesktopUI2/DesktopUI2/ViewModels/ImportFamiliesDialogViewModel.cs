using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DesktopUI2.Views.Windows.Dialogs;

namespace DesktopUI2.ViewModels
{
  public class ImportFamiliesDialogViewModel : ReactiveObject
  {

    public event EventHandler OnRequestClose;

    public static ImportFamiliesDialogViewModel Instance { get; private set; }

    private List<Symbol> _familyTypes;
    public List<Symbol> FamilyTypes
    {
      get => _familyTypes;
      set => this.RaiseAndSetIfChanged(ref _familyTypes, value);
    }
    public Dictionary<string, List<Symbol>> allSymbols { get; private set; }
    public ObservableCollection<Symbol> selectedFamilySymbols { get; set; } = new ObservableCollection<Symbol>();
    
    private string _searchQuery;
    public string SearchQuery 
    {
      get => _searchQuery;
      set
      {
        isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        FamilyTypes = new List<Symbol>(allSymbols[SelectedFamily].Where(v => v.Name.ToLower().Contains(SearchQuery.ToLower())).ToList());
        isSearching = false;
      }
    }

    public bool isSearching = false;

    private string _selectedFamily;
    public string SelectedFamily
    {
      get => _selectedFamily;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedFamily, value);
        FamilyTypes = allSymbols[value];
      }
    }

    public ObservableCollection<string> LoadedFamilies { get; set; } = new ObservableCollection<string>();

    public ImportFamiliesDialogViewModel(Dictionary<string,List<Symbol>> allSymbols)
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
          "WF", new List<Symbol>
          {
            new Symbol("W12x19", "WF"),
            new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"),
            new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"),
            new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"),
            new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"), new Symbol("W12x19", "WF"),
            new Symbol("W12x19", "WF")
          }
        },
        {
          "diff", new List<Symbol>
          {
            new Symbol("W12x21", "diff", true), new Symbol("W12x35", "diff",true), new Symbol("W12x40", "diff"),
            new Symbol("W12x19", "diff"),
          }
        }
      };

      foreach (var symbol in allSymbols.Keys)
        LoadedFamilies.Add(symbol);
      if (LoadedFamilies.Count > 0)
        SelectedFamily = LoadedFamilies[0];
    }

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

    public class Symbol : ReactiveObject
    {
      public string Name { get; set; }
      public bool isImported { get; set; }
      public string FamilyName { get; set; }

      private bool _isChecked = false;
      public bool isChecked 
      { 
        get => _isChecked;
        set
        {
          this.RaiseAndSetIfChanged(ref _isChecked, value);
          if (value == true)
            ImportFamiliesDialogViewModel.Instance.selectedFamilySymbols.Add(this);
          else if (value == false && Instance.selectedFamilySymbols.Contains(this))
            ImportFamiliesDialogViewModel.Instance.selectedFamilySymbols.Remove(this);
        }
      }
      public Symbol(string name, string familyName, bool isImported = false)
      {
        Name = name;
        FamilyName = familyName;
        this.isImported = isImported;
      }
    }
  }
}
