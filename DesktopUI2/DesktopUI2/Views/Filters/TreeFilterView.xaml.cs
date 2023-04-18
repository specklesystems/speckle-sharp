using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Filters;

public class TreeFilterView : ReactiveUserControl<FilterViewModel>
{
  public TreeFilterView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  private void TreeView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    try
    {
      (DataContext as FilterViewModel).RaisePropertyChanged("Summary");
    }
    catch
    {
      // ignore
    }
  }
}
