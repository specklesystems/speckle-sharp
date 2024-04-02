using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using ConnectorRevit;
using RevitSharedResources.Extensions.SpeckleExtensions;
using RevitSharedResources.Models;
using Speckle.BatchUploader.Sdk;
using Speckle.BatchUploader.OperationDriver;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Autodesk.Revit.DB.Events;

namespace Speckle.ConnectorRevit.Entry;

public class App : IExternalApplication
{
  public static UIApplication AppInstance { get; set; }

  public static UIControlledApplication UICtrlApp { get; set; }
  private static ConnectorBindingsRevit Bindings { get; set; }
  private bool _initialized;

  public Result OnStartup(UIControlledApplication application)
  {
    InitializeConnector();

    //Always initialize RevitTask ahead of time within Revit API context
    APIContext.Initialize(application);

    UICtrlApp = application;
    UICtrlApp.ControlledApplication.ApplicationInitialized += ControlledApplication_ApplicationInitialized;
    UICtrlApp.ControlledApplication.DocumentOpening += ControlledApplication_DocumentOpening;
    string tabName = "Speckle";

    try
    {
      application.CreateRibbonTab(tabName);
    }
    catch (Autodesk.Revit.Exceptions.ArgumentException)
    {
      // exception occurs when the speckle tab has already been created.
      // this happens when both the dui2 and the dui3 connectors are installed. Can be safely ignored.
    }
    catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
    {
      SpeckleLog.Logger.Warning(ex, "User has too many Revit add-on tabs installed");
      NotifyUserOfErrorStartingConnector(ex);
      return Result.Failed;
    }

    var specklePanel = application.CreateRibbonPanel(tabName, "Speckle 2");

    string path = typeof(App).Assembly.Location;

    //desktopui 2
    var speckleButton2 =
      specklePanel.AddItem(
        new PushButtonData(
          "Speckle 2",
          "Revit Connector",
          typeof(App).Assembly.Location,
          typeof(SpeckleRevitCommand).FullName
        )
      ) as PushButton;

    if (speckleButton2 != null)
    {
      speckleButton2.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
      speckleButton2.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
      speckleButton2.ToolTipImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);
      speckleButton2.ToolTip = "Speckle Connector for Revit";
      speckleButton2.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
      speckleButton2.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
    }

    var schedulerButton =
      specklePanel.AddItem(
        new PushButtonData("Scheduler", "Scheduler", typeof(App).Assembly.Location, typeof(SchedulerCommand).FullName)
      ) as PushButton;

    if (schedulerButton != null)
    {
      schedulerButton.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.scheduler16.png", path);
      schedulerButton.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.scheduler32.png", path);
      schedulerButton.ToolTipImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.scheduler32.png", path);
      schedulerButton.ToolTip = "Scheduler for the Revit Connector";
      schedulerButton.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
      schedulerButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
    }

    PulldownButton helpPulldown =
      specklePanel.AddItem(new PulldownButtonData("Help&Resources", "Help & Resources")) as PulldownButton;
    helpPulldown.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.help16.png", path);
    helpPulldown.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.help32.png", path);

    PushButton forum =
      helpPulldown.AddPushButton(
        new PushButtonData("forum", "Community Forum", typeof(App).Assembly.Location, typeof(ForumCommand).FullName)
      ) as PushButton;
    forum.ToolTip = "Check out our Community Forum! Opens a page in your web browser.";
    forum.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.forum16.png", path);
    forum.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.forum32.png", path);

    PushButton tutorials =
      helpPulldown.AddPushButton(
        new PushButtonData("tutorials", "Tutorials", typeof(App).Assembly.Location, typeof(TutorialsCommand).FullName)
      ) as PushButton;
    tutorials.ToolTip = "Check out our tutorials! Opens a page in your web browser.";
    tutorials.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.tutorials16.png", path);
    tutorials.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.tutorials32.png", path);

    PushButton docs =
      helpPulldown.AddPushButton(
        new PushButtonData("docs", "Docs", typeof(App).Assembly.Location, typeof(DocsCommand).FullName)
      ) as PushButton;
    docs.ToolTip = "Check out our documentation! Opens a page in your web browser.";
    docs.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.docs16.png", path);
    docs.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.docs32.png", path);

    PushButton manager =
      helpPulldown.AddPushButton(
        new PushButtonData("manager", "Manager", typeof(App).Assembly.Location, typeof(ManagerCommand).FullName)
      ) as PushButton;
    manager.ToolTip = "Manage accounts and connectors. Opens SpeckleManager.";
    manager.Image = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo16.png", path);
    manager.LargeImage = LoadPngImgSource("Speckle.ConnectorRevit.Assets.logo32.png", path);

    return Result.Succeeded;
  }

  private void ControlledApplication_ApplicationInitialized(
    object sender,
    Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e
  )
  {
    try
    {
      InitializeConnector();

      AppInstance ??= new UIApplication(sender as Application);
      CreateBindings();

      BatchUploaderClient client = new(new Uri("http://localhost:5001"));
      RevitApplicationController revitAppController = new(AppInstance);
      BatchUploadOperationDriver batchUploadOperationDriver = new(client, revitAppController, Bindings);

      batchUploadOperationDriver.ProcessAllJobs();

      //This is also called in DUI, adding it here to know how often the connector is loaded and used
      Analytics.TrackEvent(Analytics.Events.Registered, null, false);

      SpeckleRevitCommand.RegisterPane();

      //AppInstance.ViewActivated += new EventHandler<ViewActivatedEventArgs>(Application_ViewActivated);
    }
    catch (KitException ex)
    {
      SpeckleLog.Logger.Warning(ex, "Error loading kit on startup");
      NotifyUserOfErrorStartingConnector(ex);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to load Speckle app");
      NotifyUserOfErrorStartingConnector(ex);
    }
  }

  private void ControlledApplication_DocumentOpening(object sender, DocumentOpeningEventArgs e)
  {
    // When a user double-clicks on an .rvt file that start Revit, Revit invokes the DocumentOpening event before ApplicationInitialized.
    // In such instances, it becomes necessary to instantiate the pane during the document opening process.
    try
    {
      AppInstance ??= new UIApplication(sender as Application);
      CreateBindings();
      SpeckleRevitCommand.RegisterPane();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to load Speckle app");
      NotifyUserOfErrorStartingConnector(ex);
    }
  }

  private static void CreateBindings()
  {
    // Since this function is triggered during every 'DocumentOpening' event,
    // if the bindings have already been set previously, there's no need to re-set them.
    if (Bindings != null)
    {
      return;
    }

    Bindings = new ConnectorBindingsRevit(AppInstance);
    Bindings.RegisterAppEvents();
    SpeckleRevitCommand.Bindings = Bindings;
    SchedulerCommand.Bindings = Bindings;
  }

  private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
  {
    SpeckleLog.Logger.Fatal(
      e.Exception,
      "Caught thread exception with message {exceptionMessage}",
      e.Exception.Message
    );
  }

  private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
  {
    if (e.ExceptionObject is Exception ex)
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Caught unhandled exception. Is terminating : {isTerminating}. Message : {exceptionMessage}",
        e.IsTerminating,
        ex.Message
      );
    }
    else
    {
      SpeckleLog.Logger.Fatal(
        "Caught unhandled exception. Is terminating : {isTerminating}. Exception object is of type : {exceptionObjectType}. Exception object to string : {exceptionObjToString}",
        e.IsTerminating,
        e.ExceptionObject.GetType(),
        e.ExceptionObject.ToString()
      );
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
      PngBitmapDecoder m_decoder = new(icon, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
      ImageSource m_source = m_decoder.Frames[0];
      return (m_source);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
    }

    return null;
  }

  static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly a = null;
    var name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

    string assemblyFile = Path.Combine(path, name + ".dll");

    if (File.Exists(assemblyFile))
    {
      a = Assembly.LoadFrom(assemblyFile);
    }

    return a;
  }

  internal static void NotifyUserOfErrorStartingConnector(Exception ex)
  {
    using var td = new TaskDialog("Error loading Speckle");

    td.MainContent =
      ex is KitException
        ? ex.Message
        : $"Oh no! Something went wrong while loading Speckle, please report it on the forum:\n\n{ex.Message}";

    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Ask for help on our Community Forum");

    TaskDialogResult tResult = td.Show();

    if (TaskDialogResult.CommandLink1 == tResult)
    {
      Process.Start("https://speckle.community/");
    }
  }

  private void InitializeConnector()
  {
    if (_initialized)
    {
      return;
    }

    // We need to hook into the AssemblyResolve event before doing anything else
    // or we'll run into unresolved issues loading dependencies
    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    System.Windows.Forms.Application.ThreadException += Application_ThreadException;

    // initialize the speckle logger
    Setup.Init(ConnectorBindingsRevit.HostAppNameVersion, ConnectorBindingsRevit.HostAppName);

    //DUI2 - pre build app, so that it's faster to open up
    SpeckleRevitCommand.InitAvalonia();

    _initialized = true;
  }
}
