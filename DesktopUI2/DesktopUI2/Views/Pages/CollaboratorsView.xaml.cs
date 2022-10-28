using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages
{
  public partial class CollaboratorsView : ReactiveUserControl<CollaboratorsViewModel>
  {
    public static CollaboratorsView Instance { get; private set; }
    public CollaboratorsView()
    {
      InitializeComponent();
      Instance = this;
    }

    private void InitializeComponent()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
    }
  }
}
