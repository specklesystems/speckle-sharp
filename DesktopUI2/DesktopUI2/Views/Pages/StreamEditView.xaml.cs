using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Pages
{
  public partial class StreamEditView : ReactiveUserControl<StreamViewModel>
  {
    public StreamEditView()
    {
      InitializeComponent();
      Instance = this;
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public static StreamEditView Instance { get; private set; }

  }
}
