using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;
using System.Collections.Generic;

namespace DesktopUI2.Views
{
  public partial class Scheduler : ReactiveWindow<SchedulerViewModel>
  {


    public Scheduler()
    {

      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
      Instance = this;

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Launched Scheduler" } });


#if DEBUG
      this.AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif
    }

    public static Scheduler Instance { get; private set; }


    //protected override void OnClosing(CancelEventArgs e)
    //{
    //  this.Hide();
    //  e.Cancel = true;
    //  base.OnClosing(e);
    //}
  }
}
