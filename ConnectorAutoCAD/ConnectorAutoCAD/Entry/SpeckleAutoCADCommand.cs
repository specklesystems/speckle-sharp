using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Speckle.DesktopUI;
using Speckle.ConnectorAutoCAD.UI;

namespace Speckle.ConnectorAutoCAD.Entry
{
  public class SpeckleAutoCADCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsAutoCAD Bindings { get; set; }


    [CommandMethod("Speckle")]
    public static void Speckle()
    {
      try
      {
        if (Bootstrapper != null)
        {
          Bootstrapper.Application.MainWindow.Show();
          return;
        }

        Bootstrapper = new Bootstrapper()
        {
          Bindings = new ConnectorBindingsAutoCAD()
        };

        Bootstrapper.Setup(System.Windows.Application.Current);
        Bootstrapper.Start(new string[] { });

        Bootstrapper.Application.MainWindow.Initialized += (o, e) =>
        {
          ((ConnectorBindingsAutoCAD)Bootstrapper.Bindings).GetFileContextAndNotifyUI();
        };

        Bootstrapper.Application.MainWindow.Closing += (object sender, System.ComponentModel.CancelEventArgs e) =>
        {
          Bootstrapper.Application.MainWindow.Hide();
          e.Cancel = true;
        };

        var helper = new System.Windows.Interop.WindowInteropHelper(Bootstrapper.Application.MainWindow);
        helper.Owner = Application.MainWindow.Handle;

      }
      catch (System.Exception e)
      {

      }
    }
  }
}
