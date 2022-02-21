﻿using System;
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
using Revit.Async;

namespace Speckle.ConnectorRevit.Entry
{
  public class App : IExternalApplication
  {

    public static UIApplication AppInstance { get; set; }

    public static UIControlledApplication UICtrlApp { get; set; }

    public Result OnStartup(UIControlledApplication application)
    {
      //Always initialize RevitTask ahead of time within Revit API context
      RevitTask.Initialize();

      UICtrlApp = application;
      // Fires an init event, where we can get the UIApp
      UICtrlApp.Idling += Initialise;

      var specklePanel = application.CreateRibbonPanel("Speckle 2");
      var speckleButton = specklePanel.AddItem(new PushButtonData("Speckle 2 (old)", "Revit Connector (old)", typeof(App).Assembly.Location, typeof(SpeckleRevitCommand).FullName)) as PushButton;
      string path = typeof(App).Assembly.Location;

      if (speckleButton != null)
      {
        speckleButton.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
        speckleButton.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        speckleButton.ToolTip = "Speckle Connector for Revit (old)";
        speckleButton.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        speckleButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
      }

      //desktopui 2
      var speckleButton2 = specklePanel.AddItem(new PushButtonData("Speckle 2", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitCommand2).FullName)) as PushButton;

      if (speckleButton2 != null)
      {
        speckleButton2.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
        speckleButton2.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
        speckleButton2.ToolTip = "Speckle Connector for Revit";
        speckleButton2.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
        speckleButton2.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
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

    private void Initialise(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
      UICtrlApp.Idling -= Initialise;
      AppInstance = sender as UIApplication;

      AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

      // Set up bindings now as they subscribe to some document events and it's better to do it now
      SpeckleRevitCommand.Bindings = new ConnectorBindingsRevit(AppInstance);
      var eventHandler = ExternalEvent.Create(new SpeckleExternalEventHandler(SpeckleRevitCommand.Bindings));
      SpeckleRevitCommand.Bindings.SetExecutorAndInit(eventHandler);

      //pre build app, so that it's faster to open up
      SpeckleRevitCommand2.InitAvalonia();
      SpeckleRevitCommand2.Bindings = new ConnectorBindingsRevit2(AppInstance);
      SpeckleRevitCommand2.Bindings.RegisterAppEvents();

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
