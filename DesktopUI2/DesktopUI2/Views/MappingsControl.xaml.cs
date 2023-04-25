using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels.MappingTool;
using ReactiveUI;
using Speckle.Core.Logging;

namespace DesktopUI2.Views;

public class MappingsControl : ReactiveUserControl<MappingsViewModel>
{
  public MappingsControl()
  {
    Instance = this;
    this.WhenActivated(disposables => { });
    AvaloniaXamlLoader.Load(this);

    Analytics.TrackEvent(
      Analytics.Events.MappingsAction,
      new Dictionary<string, object> { { "name", "Mappings Launched" } }
    );
  }

  internal static MappingsControl Instance { get; private set; }

  //these methods are here as it wasn't easy to have them in the MappingsViewModel

  private void PointerEnterEvent(object sender, PointerEventArgs e)
  {
    var dc = (sender as Control).DataContext as SchemaGroup;
    MappingsViewModel.Instance.Bindings.HighlightElements(dc.Schemas.Select(x => x.ApplicationId).ToList());
  }

  private void PointerEnterEventItem(object sender, PointerEventArgs e)
  {
    var dc = (sender as Control).DataContext as Schema;
    MappingsViewModel.Instance.Bindings.HighlightElements(new List<string> { dc.ApplicationId });
  }

  private void PointerLeaveEvent(object sender, PointerEventArgs e)
  {
    MappingsViewModel.Instance.Bindings.HighlightElements(new List<string>());
  }
}
