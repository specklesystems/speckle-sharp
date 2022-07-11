using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.PlugIns;

namespace SpeckleRhino
{
  public class SpeckleRhinoConnectorPlugin : PlugIn
  {
    public static SpeckleRhinoConnectorPlugin Instance { get; private set; }

    private List<string> ExistingStreams = new List<string>(); // property for tracking stream data during copy and import operations

    private static string SpeckleKey = "speckle2";

    public ConnectorBindingsRhino Bindings { get; private set; }
    public MainViewModel ViewModel { get; private set; }

    public SpeckleRhinoConnectorPlugin()
    {
      Instance = this;
#if !DEBUG
      Init();
#endif
    }

    internal void Init()
    {

      SpeckleCommand.InitAvalonia();
      Bindings = new ConnectorBindingsRhino();
      ViewModel = new MainViewModel(Bindings);

      RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;

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
      SpeckleCommand.CreateOrFocusSpeckle();
#else
        Rhino.UI.Panels.OpenPanel(typeof(Panel).GUID);
#endif

      }
    }

    private void RhinoDoc_BeginOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      //new document => new view model (used by the panel only)
      ViewModel = new MainViewModel(Bindings);

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
#if !MAC
      System.Type panelType = typeof(Panel);
      // Register my custom panel class type with Rhino, the custom panel my be display
      // by running the MyOpenPanel command and hidden by running the MyClosePanel command.
      // You can also include the custom panel in any existing panel group by simply right
      // clicking one a panel tab and checking or un-checking the "MyPane" option.
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
          sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
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
