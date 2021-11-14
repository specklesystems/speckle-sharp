using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Stylet.Xaml;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;

using Speckle.ConnectorMicroStationOpenRoads.UI;

namespace Speckle.ConnectorMicroStationOpenRoads.Entry
{
  [AddIn(MdlTaskID = "Speckle")]
  public class SpeckleMicroStationOpenRoadsApp : AddIn
  {
    public static SpeckleMicroStationOpenRoadsApp App;

    public SpeckleMicroStationOpenRoadsApp(IntPtr mdlDesc) : base(mdlDesc)
    {
    }

    protected override int Run(string[] commandLine)
    {
      App = this;
      return 0;
    }

    internal static SpeckleMicroStationOpenRoadsApp Instance
    {
      get { return App; }
    }

    public void Start(string unparsed)
    {
      SpeckleMicroStationOpenRoadsCommand.ShowPanel();
    }
  }

  public class SpeckleMicroStationOpenRoadsCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsMicroStationOpenRoads Bindings { get; set; }
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
          Bindings = new ConnectorBindingsMicroStationOpenRoads()
        };

        if (Application.Current != null)
          new StyletAppLoader() { Bootstrapper = Bootstrapper };
        else
          new DesktopUI.App(Bootstrapper);

        Bootstrapper.Start(Application.Current);
      }
      catch (Exception e)
      {
        Bootstrapper = null;
      }
    }
  }
}