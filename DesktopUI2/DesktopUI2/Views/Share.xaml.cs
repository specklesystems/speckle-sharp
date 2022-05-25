using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels.Share;

namespace DesktopUI2.Views
{
  public partial class Share : ReactiveWindow<ShareViewModel>
  {

    public Share()
    {
      InitializeComponent();
#if DEBUG
      this.AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif


    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
