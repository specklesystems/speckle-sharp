using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Revit.Async;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  public class App : IExternalApplication
  {

    public static UIApplication AppInstance { get; set; }

    public static UIControlledApplication UICtrlApp { get; set; }

    public Result OnStartup(UIControlledApplication application)
    {
      //Always initialize RevitTask ahead of time within Revit API context
      RevitTask.Initialize(application);

      UICtrlApp = application;
      // Fires an init event, where we can get the UIApp
      UICtrlApp.Idling += Initialise;

      string tabName = "Speckle";

      try
      {
        application.CreateRibbonTab(tabName);
      }
      catch { }

      var specklePanel = application.CreateRibbonPanel(tabName, "Speckle 2");

      string path = typeof(App).Assembly.Location;
#if REVIT2019
      //desctopui 1
      var speckleButton = specklePanel.AddItem(new PushButtonData("Speckle 2", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitCommand).FullName)) as PushButton;

      if (speckleButton != null)
      {
        speckleButton.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16_fade.png", path);
        speckleButton.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32_fade.png", path);
        speckleButton.ToolTip = "Speckle Connector for Revit (old)";
        speckleButton.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        speckleButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }
#else

      //TODO: remove
      //addin tab placeholder => to be delete ina  couple of releases
      var tempSpecklePanel = application.CreateRibbonPanel("Speckle 2");
      var placeholderSpeckleButton2 = tempSpecklePanel.AddItem(new PushButtonData("Speckle 2 placeholder", "Revit Connector", typeof(App).Assembly.Location, typeof(NewRibbonCommand).FullName)) as PushButton;

      if (placeholderSpeckleButton2 != null)
      {
        placeholderSpeckleButton2.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
        placeholderSpeckleButton2.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        placeholderSpeckleButton2.ToolTipImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        placeholderSpeckleButton2.ToolTip = "The Speckle Connector has moved and this button will be removed soon.";
        placeholderSpeckleButton2.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        placeholderSpeckleButton2.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }




      //desktopui 2
      var speckleButton2 = specklePanel.AddItem(new PushButtonData("Speckle 2", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitCommand2).FullName)) as PushButton;

      if (speckleButton2 != null)
      {
        speckleButton2.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
        speckleButton2.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        speckleButton2.ToolTipImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        speckleButton2.ToolTip = "Speckle Connector for Revit";
        speckleButton2.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        speckleButton2.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }

      var schedulerButton = specklePanel.AddItem(new PushButtonData("Scheduler", "Scheduler", typeof(App).Assembly.Location, typeof(SchedulerCommand).FullName)) as PushButton;

      if (schedulerButton != null)
      {
        schedulerButton.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.scheduler16.png", path);
        schedulerButton.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.scheduler32.png", path);
        schedulerButton.ToolTipImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.scheduler32.png", path);
        schedulerButton.ToolTip = "Scheduler for the Revit Connector";
        schedulerButton.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        schedulerButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }

      // one click send
      var speckleButtonSend = specklePanel.AddItem(new PushButtonData("Send", "Send to Speckle", typeof(App).Assembly.Location, typeof(OneClickSendCommand).FullName)) as PushButton;

      if (speckleButtonSend != null)
      {
        speckleButtonSend.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.oneclick16.png", path);
        speckleButtonSend.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.oneclick32.png", path);
        speckleButtonSend.ToolTipImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.oneclick32.png", path);
        speckleButtonSend.ToolTip = "Sends your selected file objects to Speckle, or the entire model if nothing is selected.";
        speckleButtonSend.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        speckleButtonSend.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }
#endif

      PulldownButton helpPulldown = specklePanel.AddItem(new PulldownButtonData("Help&Resources", "Help & Resources")) as PulldownButton;
      helpPulldown.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.help16.png", path);
      helpPulldown.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.help32.png", path);

      PushButton forum = helpPulldown.AddPushButton(new PushButtonData("forum", "Community Forum", typeof(App).Assembly.Location, typeof(ForumCommand).FullName)) as PushButton;
      forum.ToolTip = "Check out our Community Forum! Opens a page in your web browser.";
      forum.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.forum16.png", path);
      forum.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.forum32.png", path);

      PushButton tutorials = helpPulldown.AddPushButton(new PushButtonData("tutorials", "Tutorials", typeof(App).Assembly.Location, typeof(TutorialsCommand).FullName)) as PushButton;
      tutorials.ToolTip = "Check out our tutorials! Opens a page in your web browser.";
      tutorials.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.tutorials16.png", path);
      tutorials.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.tutorials32.png", path);

      PushButton docs = helpPulldown.AddPushButton(new PushButtonData("docs", "Docs", typeof(App).Assembly.Location, typeof(DocsCommand).FullName)) as PushButton;
      docs.ToolTip = "Check out our documentation! Opens a page in your web browser.";
      docs.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.docs16.png", path);
      docs.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.docs32.png", path);

      PushButton manager = helpPulldown.AddPushButton(new PushButtonData("manager", "Manager", typeof(App).Assembly.Location, typeof(ManagerCommand).FullName)) as PushButton;
      manager.ToolTip = "Manage accounts and connectors. Opens SpeckleManager.";
      manager.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
      manager.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);

      return Result.Succeeded;
    }

    private void Initialise(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
      UICtrlApp.Idling -= Initialise;
      AppInstance = sender as UIApplication;
      AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);


#if REVIT2019
      //DUI1 - Set up bindings now as they subscribe to some document events and it's better to do it now
      SpeckleRevitCommand.Bindings = new ConnectorBindingsRevit(AppInstance);
      var eventHandler = ExternalEvent.Create(new SpeckleExternalEventHandler(SpeckleRevitCommand.Bindings));
      SpeckleRevitCommand.Bindings.SetExecutorAndInit(eventHandler);
#else
      //DUI2 - pre build app, so that it's faster to open up
      SpeckleRevitCommand2.uiapp = AppInstance;
      SpeckleRevitCommand2.InitAvalonia();
      var bindings = new ConnectorBindingsRevit2(AppInstance);
      bindings.RegisterAppEvents();
      SpeckleRevitCommand2.Bindings = bindings;
      SchedulerCommand.Bindings = bindings;
      OneClickSendCommand.Bindings = bindings;
#endif

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

    static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

      return a;
    }
  }

}
