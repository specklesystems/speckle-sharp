using System;
using System.IO;
using System.Reflection;
using System.Windows;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;

using Speckle.ConnectorBentley.UI;
using Speckle.DesktopUI;
using Stylet.Xaml;

namespace Speckle.ConnectorBentley.Entry
{
  public class SpeckleBentleyCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsBentley Bindings { get; set; }
    public static void ShowPanel()
    {
      try
      {
        if (Bootstrapper != null)
        {
          Bootstrapper.ShowRootView();
          return;
        }

        Bootstrapper = new Bootstrapper()
        {
          Bindings = new ConnectorBindingsBentley()
        };

        if (System.Windows.Application.Current != null)
          new StyletAppLoader() { Bootstrapper = Bootstrapper };
        else
          new DesktopUI.App(Bootstrapper);

        Bootstrapper.Start(System.Windows.Application.Current);
      }
      catch (Exception e)
      {
        Bootstrapper = null;
      }
    }
  }
}
