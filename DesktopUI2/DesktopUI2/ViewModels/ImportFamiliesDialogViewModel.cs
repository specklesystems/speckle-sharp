using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Views;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Splat;

using System.Diagnostics;

namespace DesktopUI2.ViewModels
{
  public class ImportFamiliesDialogViewModel : ReactiveObject
  {

    public event EventHandler OnRequestClose;

    private ConnectorBindings Bindings;

    public static ImportFamiliesDialogViewModel Instance { get; private set; }

    public ObservableCollection<Symbol> familySymbols { get; set; }
    private List<Symbol> _selectedFamilySymbols { get; set; }
    public ObservableCollection<Symbol> selectedFamilySymbols { get; set; } = new ObservableCollection<Symbol>();
    public string SearchQuery { get; set; }

    public bool isOpen = true;

    public ImportFamiliesDialogViewModel(List<string> allSymbols, List<string> importedSymbols)
    {
      Instance = this;
      familySymbols = new ObservableCollection<Symbol>();
      foreach (var symbol in allSymbols)
      {
        if (importedSymbols.Contains(symbol))
          familySymbols.Add(new Symbol(symbol, true));
        else
          familySymbols.Add(new Symbol(symbol));
      }
    }

    // this constructor is only for xaml design purposes
    public ImportFamiliesDialogViewModel()
    {
      Instance = this;
      familySymbols = new ObservableCollection<Symbol>
      {
        new Symbol("W12x19"), new Symbol("W12x21", true), new Symbol("W12x35", true), 
        new Symbol("W12x40"), new Symbol("W12x19"), new Symbol("W12x19"), new Symbol("W12x19"), 
        new Symbol("W12x19"), new Symbol("W12x19"), new Symbol("W12x19"), new Symbol("W12x19"), 
        new Symbol("W12x19"), new Symbol("W12x19"), new Symbol("W12x19"), new Symbol("W12x19"), 
        new Symbol("W12x19"), new Symbol("W12x19"), new Symbol("W12x19"),
      };
    }

    public class Symbol : ReactiveObject
    {
      public string Name { get; set; }
      public bool isImported { get; set; }

      private bool _isChecked;
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
      public Symbol(string name, bool isImported = false)
      {
        Name = name;
        this.isImported = isImported;
        isChecked = false;
      }
    }

    public void ImportSymbolsCommand()
    {
      isOpen = false;
      OnRequestClose(this, new EventArgs());
    }

    public void Close_Click()
    {
      selectedFamilySymbols = new ObservableCollection<Symbol>();
      isOpen = false;
      OnRequestClose(this, new EventArgs());
    }
  }
}
