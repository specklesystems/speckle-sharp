using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Filters;

public class AllFilterView : ReactiveUserControl<FilterViewModel>
{
  public AllFilterView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
