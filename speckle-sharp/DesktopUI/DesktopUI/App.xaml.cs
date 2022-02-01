using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Speckle.DesktopUI.Properties;

namespace Speckle.DesktopUI
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    public App()
    {
      var bootstrapper = new Bootstrapper();
      Resources.MergedDictionaries.Add(new StyletAppLoader() {Bootstrapper = bootstrapper});
      bootstrapper.Start(Current);
    }

    public App(Bootstrapper bootstrapper)
    {
      Resources.MergedDictionaries.Add(new StyletAppLoader() {Bootstrapper = bootstrapper});
    }
  }
}
