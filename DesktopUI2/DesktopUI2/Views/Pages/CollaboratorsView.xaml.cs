using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages;

public class CollaboratorsView : ReactiveUserControl<CollaboratorsViewModel>
{
  public CollaboratorsView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    this.WhenActivated(disposables => { });
    AvaloniaXamlLoader.Load(this);
  }
}
