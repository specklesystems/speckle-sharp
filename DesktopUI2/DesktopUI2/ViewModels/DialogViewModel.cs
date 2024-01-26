using DesktopUI2.Models;
using ReactiveUI;

namespace DesktopUI2.ViewModels;

public class DialogViewModel : ReactiveObject
{
  //UI Binding
  public bool UseFe2
  {
    get
    {
      var config = ConfigManager.Load();
      return config.UseFe2;
    }
  }
}
