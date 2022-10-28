﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.ReactiveUI;
using ConnectorRhinoShared;
using Rhino;
using Rhino.PlugIns;
using Rhino.Runtime;
using Speckle.Core.Api;
using Speckle.Core.Models.Extensions;

[assembly: Guid("8dd5f30b-a13d-4a24-abdc-3e05c8c87143")]

namespace SpeckleRhino
{
  public class SpeckleRhinoConnectorPlugin : PlugIn
  {
    public static SpeckleRhinoConnectorPlugin Instance { get; private set; }

    private List<string> ExistingStreams = new List<string>(); // property for tracking stream data during copy and import operations

    private static string SpeckleKey = "speckle2";

    public ConnectorBindingsRhino Bindings { get; private set; }


    public static AppBuilder appBuilder;

    public SpeckleRhinoConnectorPlugin()
    {
      Instance = this;
    }

    public void Init()
    {
      try
      {
        if (appBuilder != null)
          return;

#if MAC
        InitAvaloniaMac();
#else
        appBuilder = BuildAvaloniaApp().SetupWithoutStarting();
#endif


        Bindings = new ConnectorBindingsRhino();

        RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
        RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
      }
      catch (Exception ex)
      {
        RhinoApp.CommandLineOut.WriteLine($"Speckle error — {ex.ToFormattedString()}");
      }

    }


    public static void InitAvaloniaMac()
    {
      var rhinoMenuPtr = MacOSHelpers.MainMenu;
      var rhinoDelegate = MacOSHelpers.AppDelegate;
      var titlePtr = MacOSHelpers.MenuItemGetTitle(MacOSHelpers.MenuItemGetSubmenu(MacOSHelpers.MenuItemAt(rhinoMenuPtr, 0)));

      appBuilder = BuildAvaloniaApp().SetupWithoutStarting();

      // don't use Avalonia's AppDelegate.. not sure what consequences this might have to Avalonia functionality
      MacOSHelpers.AppDelegate = rhinoDelegate;
      MacOSHelpers.MainMenu = rhinoMenuPtr;
      MacOSHelpers.MenuItemSetTitle(MacOSHelpers.MenuItemGetSubmenu(MacOSHelpers.MenuItemAt(rhinoMenuPtr, 0)), MacOSHelpers.NewObject("NSString"));
      MacOSHelpers.MenuItemSetTitle(MacOSHelpers.MenuItemGetSubmenu(MacOSHelpers.MenuItemAt(rhinoMenuPtr, 0)), titlePtr);

    }

    public static AppBuilder BuildAvaloniaApp()
    {
      return AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new X11PlatformOptions { UseGpu = false })
      .With(new AvaloniaNativePlatformOptions { UseGpu = false, UseDeferredRendering = true })
      .With(new MacOSPlatformOptions { ShowInDock = false, DisableDefaultApplicationMenuItems = true, DisableNativeMenus = true })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .LogToTrace()
      .UseReactiveUI();
    }

    private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge) // this is a paste or import event
      {
        // get incoming streams
        var incomingStreams = e.Document.Strings.GetEntryNames(SpeckleKey);

        // remove any that don't already exist in the current active doc
        foreach (var incomingStream in incomingStreams)
          if (!ExistingStreams.Contains(incomingStream))
            RhinoDoc.ActiveDoc.Strings.Delete(SpeckleKey, incomingStream);

        // skip binding
        return;
      }

      if (Bindings.GetStreamsInFile().Count > 0)
      {
#if MAC
        try
        {
          SpeckleCommandMac.CreateOrFocusSpeckle();
        } catch (Exception ex)
        {
          RhinoApp.CommandLineOut.WriteLine($"Speckle error - {ex.ToFormattedString()}");
        }
#else
        Rhino.UI.Panels.OpenPanel(typeof(Panel).GUID);
#endif

      }
    }

    private void RhinoDoc_BeginOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge) // this is a paste or import event
      {
        // get existing streams in doc before a paste or import operation to use for cleanup
        ExistingStreams = RhinoDoc.ActiveDoc.Strings.GetEntryNames(SpeckleKey).ToList();
      }
    }

    /// <summary>
    /// Called when the plugin is being loaded. Used to delete existing .rui toolbar file on load so rhino will automatically copy and re-stage the new .rui file.
    /// </summary>
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
      string processName = "";
      System.Version processVersion = null;
      HostUtils.GetCurrentProcessInfo(out processName, out processVersion);

      // The user is probably using Rhino Inside and Avalonia was already initialized there  or will be initialized later and will throw an error
      // https://speckle.community/t/revit-command-failure-for-external-command/3489/27
      if (!processName.Equals("rhino", StringComparison.InvariantCultureIgnoreCase))
      {

        errorMessage = "Speckle does not currently support Rhino.Inside";
        RhinoApp.CommandLineOut.WriteLine(errorMessage);
        return LoadReturnCode.ErrorNoDialog;
      }


      Init();

#if !MAC
      System.Type panelType = typeof(Panel);
      Rhino.UI.Panels.RegisterPanel(this, panelType, "Speckle", Resources.icon);
#endif
      // Get the version number of our plugin, that was last used, from our settings file.
      var plugin_version = Settings.GetString("PlugInVersion", null);

      if (!string.IsNullOrEmpty(plugin_version))
      {
        // If the version number of the plugin that was last used does not match the
        // version number of this plugin, proceed.
        if (0 != string.Compare(Version, plugin_version, StringComparison.OrdinalIgnoreCase))
        {
          // Build a path to the user's staged RUI file.
          var sb = new StringBuilder();
          sb.Append(Helpers.InstallApplicationDataPath);
#if RHINO6
          sb.Append(@"\McNeel\Rhinoceros\6.0\UI\Plug-ins\");
#elif RHINO7
          sb.Append(@"\McNeel\Rhinoceros\7.0\UI\Plug-ins\");
#endif
          sb.AppendFormat("{0}.rui", Assembly.GetName().Name);

          var path = sb.ToString();
          if (File.Exists(path))
          {
            try
            {
              File.Delete(path);
            }
            catch { }
          }

          // Save the version number of this plugin to our settings file.
          Settings.SetString("PlugInVersion", Version);
        }
      }

      // After successfully loading the plugin, if Rhino detects a plugin RUI file, it will automatically stage it, if it doesn't already exist.

      return LoadReturnCode.Success;
    }
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
  }
}
