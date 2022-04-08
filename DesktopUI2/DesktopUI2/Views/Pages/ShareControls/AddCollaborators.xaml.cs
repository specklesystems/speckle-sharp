using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels.Share;
using ReactiveUI;

namespace DesktopUI2.Views.Pages.ShareControls
{
  public partial class AddCollaborators : ReactiveUserControl<AddCollaboratorsViewModel>
  {
    public static AddCollaborators Instance { get; private set; }
    public AddCollaborators()
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
