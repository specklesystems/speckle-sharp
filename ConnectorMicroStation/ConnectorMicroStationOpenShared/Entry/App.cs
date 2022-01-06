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

using Speckle.ConnectorMicroStationOpen.UI;

namespace Speckle.ConnectorMicroStationOpen.Entry
{
  [AddIn(MdlTaskID = "Speckle")]
  public class SpeckleMicroStationOpenApp : AddIn
  {
    public static SpeckleMicroStationOpenApp App;

    public SpeckleMicroStationOpenApp(IntPtr mdlDesc) : base(mdlDesc)
    {
    }

    protected override int Run(string[] commandLine)
    {
      App = this;
      return 0;
    }

    internal static SpeckleMicroStationOpenApp Instance
    {
      get { return App; }
    }

    public void Start(string unparsed)
    {
      SpeckleMicroStationOpenRoadsCommand.ShowPanel();
    }
  }
  
}