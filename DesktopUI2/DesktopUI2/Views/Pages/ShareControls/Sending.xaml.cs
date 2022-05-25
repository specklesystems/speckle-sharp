using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels.Share;

namespace DesktopUI2.Views.Pages.ShareControls
{
  public partial class Sending : ReactiveUserControl<SendingViewModel>
  {
    public Sending()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
