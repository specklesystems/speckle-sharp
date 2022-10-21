using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DesktopUI2;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.PlugIns;
using Rhino.Runtime;
using Speckle.Core.Api;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino
{
  public class SpeckleRhinoConnectorPlugin : PlugIn
  {
    public static SpeckleRhinoConnectorPlugin Instance { get; private set; }

    private List<string> ExistingStreams = new List<string>(); // property for tracking stream data during copy and import operations

    private static string SpeckleKey = "speckle2";

    public ConnectorBindingsRhino Bindings { get; private set; }

    internal bool _initialized;

    public SpeckleRhinoConnectorPlugin()
    {
      Instance = this;
    }

    public void Init()
    {
      try
      {
        if (_initialized)
          return;

        SpeckleCommand.InitAvalonia();
        Bindings = new ConnectorBindingsRhino();

        RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
        RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;

        _initialized = true;
      }
      catch (Exception ex)
      {
        RhinoApp.CommandLineOut.WriteLine($"Speckle error — {ex.ToFormattedString()}");
      }

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
          var msg = "This file contained some speckle streams, but Speckle is temporarily disabled on Rhino due to a critical bug regarding Rhino's top-menu commands. Please use Grasshopper instead while we fix this.";
          RhinoApp.CommandLineOut.WriteLine(msg);
          Rhino.UI.Dialogs.ShowMessage(msg, "Speckle has been disabled", Rhino.UI.ShowMessageButton.OK, Rhino.UI.ShowMessageIcon.Exclamation);
          //SpeckleCommand.CreateOrFocusSpeckle();
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



#if !MAC
      System.Type panelType = typeof(Panel);
      // Register my custom panel class type with Rhino, the custom panel my be display
      // by running the MyOpenPanel command and hidden by running the MyClosePanel command.
      // You can also include the custom panel in any existing panel group by simply right
      // clicking one a panel tab and checking or un-checking the "MyPane" option.
      Init();
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
#if MAC
public override PlugInLoadTime LoadTime => PlugInLoadTime.Disabled;
#else
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
#endif
  }
}
