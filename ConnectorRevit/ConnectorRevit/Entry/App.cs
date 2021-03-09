using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.Storage;
using Speckle.ConnectorRevit.UI;
using Speckle.DesktopUI;

namespace Speckle.ConnectorRevit.Entry
{
  public class App : IExternalApplication
  {

    public static UIApplication AppInstance { get; set; }

    public static UIControlledApplication UICtrlApp { get; set; }

    // public static readonly FailureDefinitionId GenericFailureGuid =
    //   new FailureDefinitionId(new Guid("bcada956-0645-4a09-b264-f2154444c677"));

    public Result OnStartup(UIControlledApplication application)
    {
      UICtrlApp = application;
      // Fires an init event, where we can get the UIApp
      UICtrlApp.Idling += Initialise;

      var SpecklePanel = application.CreateRibbonPanel("Speckle 2");
      var SpeckleButton = SpecklePanel.AddItem(new PushButtonData("Speckle 2", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitCommand).FullName))as PushButton;

      if (SpeckleButton != null)
      {
        string path = typeof(App).Assembly.Location;
        SpeckleButton.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
        SpeckleButton.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        SpeckleButton.ToolTip = "Speckle Connector for Revit";
        SpeckleButton.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        SpeckleButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }

      // var failDef = FailureDefinition.CreateFailureDefinition(
      //   GenericFailureGuid, FailureSeverity.DocumentCorruption,
      //   "Generic Speckle failure to resolve unresolvable Revit errors");
      // failDef.AddResolutionType(FailureResolutionType.DeleteElements, "delete elements to resolve error",
      //   typeof(DeleteElements));
      // failDef.SetDefaultResolutionType(FailureResolutionType.DeleteElements);
      // var defRes = failDef.GetDefaultResolutionType();

      return Result.Succeeded;
    }

    private void Initialise(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
      UICtrlApp.Idling -= Initialise;
      AppInstance = sender as UIApplication;

      // Set up bindings now as they subscribe to some document events and it's better to do it now
      SpeckleRevitCommand.Bindings = new ConnectorBindingsRevit(AppInstance);
      var eventHandler = ExternalEvent.Create(new SpeckleExternalEventHandler(SpeckleRevitCommand.Bindings));
      SpeckleRevitCommand.Bindings.SetExecutorAndInit(eventHandler);
    }

    public Result OnShutdown(UIControlledApplication application)
    {
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
