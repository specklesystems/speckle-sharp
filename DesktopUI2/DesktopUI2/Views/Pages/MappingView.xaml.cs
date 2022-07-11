using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Pages
{
  public partial class MappingView : ReactiveUserControl<MappingViewModel>
  {
    public MappingView()
    {
      InitializeComponent();
      Instance = this;
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);

    }

    public static MappingView Instance { get; private set; }

  }
}
