using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Api;

namespace DesktopUI2.Views.Windows.Dialogs;

public class NewBranchDialog : DialogUserControl
{
  public NewBranchDialog()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void Create_Click(object sender, RoutedEventArgs e)
  {
    Close(true);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close(false);
  }
}

public class NewBranchViewModel : ViewModelBase, INotifyDataErrorInfo
{
  private string _branchName;

  private string _description;
  private string _valueError;

  public NewBranchViewModel(List<Branch> branches)
  {
    Branches = branches;
    this.WhenAnyValue(x => x.BranchName).Subscribe(_ => UpdateErrors());
  }

  public List<Branch> Branches { get; set; }

  public string BranchName
  {
    get => _branchName;
    set => this.RaiseAndSetIfChanged(ref _branchName, value);
  }

  public string Description
  {
    get => _description;
    set => this.RaiseAndSetIfChanged(ref _description, value);
  }

  public bool HasErrors => throw new NotImplementedException();

  public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

  public IEnumerable GetErrors(string propertyName)
  {
    switch (propertyName)
    {
      case nameof(Branches):
        return new[] { _valueError };
      default:
        return null;
    }
  }

  private void UpdateErrors()
  {
    if (!Branches.Any(x => x.name.Equals(BranchName, StringComparison.InvariantCultureIgnoreCase)))
    {
      if (_valueError != null)
      {
        _valueError = null;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(BranchName)));
      }
    }
    else
    {
      if (_valueError == null)
      {
        _valueError = "A branch with this name already exists";
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(BranchName)));
      }
    }
  }
}
