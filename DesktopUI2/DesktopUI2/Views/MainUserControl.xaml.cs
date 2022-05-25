using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;
using System.Collections.Generic;

namespace DesktopUI2.Views
{
  public partial class MainUserControl : ReactiveUserControl<MainViewModel>
  {
    public MainUserControl()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Launched" } });



    }
  }
}
