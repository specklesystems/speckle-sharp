﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Revit.Async;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Logging;

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
      UICtrlApp.ControlledApplication.ApplicationInitialized += ControlledApplication_ApplicationInitialized;
      string tabName = "Speckle";

      try
      {
        application.CreateRibbonTab(tabName);
      }
      catch { }

      var specklePanel = application.CreateRibbonPanel(tabName, "Speckle 2");

      string path = typeof(App).Assembly.Location;

      //desktopui 2
      var speckleButton2 = specklePanel.AddItem(new PushButtonData("Speckle 2", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitCommand).FullName)) as PushButton;

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

    private void ControlledApplication_ApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
    {
      try
      {
        AppInstance = new UIApplication(sender as Application);
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

        //DUI2 - pre build app, so that it's faster to open up
        SpeckleRevitCommand.InitAvalonia();
        var bindings = new ConnectorBindingsRevit(AppInstance);
        bindings.RegisterAppEvents();
        SpeckleRevitCommand.Bindings = bindings;
        SchedulerCommand.Bindings = bindings;

        //This is also called in DUI, adding it here to know how often the connector is loaded and used
        Setup.Init(bindings.GetHostAppNameVersion(), bindings.GetHostAppName());
        Analytics.TrackEvent(Analytics.Events.Registered, null, false);

        SpeckleRevitCommand.RegisterPane();

        //AppInstance.ViewActivated += new EventHandler<ViewActivatedEventArgs>(Application_ViewActivated);
      }
      catch (Exception ex)
      {
        Serilog.Log.Error(ex, ex.Message);
        var td = new TaskDialog("Could not load Speckle");
        td.MainContent = $"Oh no! Something went wrong while loading Speckle, please report it on the forum:\n{ex.Message}";
        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Report issue on our Community Forum");

        TaskDialogResult tResult = td.Show();

        if (TaskDialogResult.CommandLink1 == tResult)
        {
          Process.Start("https://speckle.community/");
        }
      }
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
