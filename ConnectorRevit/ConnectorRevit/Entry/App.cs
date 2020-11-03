using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Speckle.ConnectorRevit.Entry
{
  public class App : IExternalApplication
  {
    public Result OnShutdown(UIControlledApplication application)
    {
      return Result.Succeeded;
    }

    public Result OnStartup(UIControlledApplication application)
    {
      var SpecklePanel = application.CreateRibbonPanel("Speckle 2");
      var SpeckleButton = SpecklePanel.AddItem(new PushButtonData("Speckle 2", "Revit Connector", typeof(App).Assembly.Location, typeof(Cmd).FullName)) as PushButton;

      if (SpeckleButton != null)
      {
        string path = typeof(App).Assembly.Location;
        SpeckleButton.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
        SpeckleButton.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        SpeckleButton.ToolTip = "Speckle Connector for Revit";
        SpeckleButton.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        SpeckleButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }

      return Result.Succeeded;
    }

    private ImageSource LoadPngImgSource(string sourceName, string path)
    {
      try
      {
        var assembly = Assembly.LoadFrom(Path.Combine(path));
        var icon = assembly.GetManifestResourceStream(sourceName);
        PngBitmapDecoder m_decoder = new PngBitmapDecoder(icon, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
        ImageSource m_source = m_decoder.Frames[0];
        return (m_source);
      }
      catch { }

      return null;
    }
  }

}
