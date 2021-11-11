using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpeckleUpdater
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private void Application_Startup(object sender, StartupEventArgs e)
    {
      try
      {
        var showProgress = false;
        if (e.Args.Length == 1 && e.Args[0] == "-showprogress")
          showProgress = true;

        MainWindow wnd = new MainWindow(showProgress);

        if (showProgress)
          wnd.Show();

      }
      catch (Exception ex)
      {
        //fail silently
      }
    }
  }
}
