using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.ViewModels.MappingTool;
using ReactiveUI;
using System.Linq;

namespace DesktopUI2.Views
{
  public partial class MappingsControl : ReactiveUserControl<MappingsViewModel>
  {
    public MappingsControl()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);


    }

    private void PointerEnterEvent(object sender, Avalonia.Input.PointerEventArgs e)
    {
      var dc = (sender as Expander).DataContext as SchemaGroup;
      MappingsViewModel.Instance.Bindings.HighlightElements(dc.Schemas.Select(x => x.ApplicationId).ToList());
    }
  }
}
