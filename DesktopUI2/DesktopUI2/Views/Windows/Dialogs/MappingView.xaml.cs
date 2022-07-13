using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class MappingView : ReactiveWindow<MappingViewModel>
  {
    public MappingView()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
      Instance = this;
    }

    public static MappingView Instance { get; private set; }
  }
}
