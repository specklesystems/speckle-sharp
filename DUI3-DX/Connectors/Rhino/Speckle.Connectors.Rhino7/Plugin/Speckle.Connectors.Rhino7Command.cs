using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.UI;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Properties;

namespace Speckle.Connectors.Rhino7.Plugin;

public class SpeckleConnectorsRhino7Command : Command
{
  public SpeckleConnectorsRhino7Command()
  {
    // Rhino only creates one instance of each command class defined in a
    // plug-in, so it is safe to store a reference in a static property.
    Instance = this;
    Panels.RegisterPanel(
      SpeckleConnectorsRhino7Plugin.Instance,
      typeof(SpeckleRhinoPanelHost),
      "Speckle (New UI)",
      Resources.speckle32,
      PanelType.System
    );
  }
  ///<summary>The only instance of this command.</summary>
  public static SpeckleConnectorsRhino7Command? Instance { get; private set; }

  ///<returns>The command name as it appears on the Rhino command line.</returns>
  public override string EnglishName => "SpeckleNewUI";

  protected override Result RunCommand(RhinoDoc doc, RunMode mode)
  {
    Guid panelId = typeof(SpeckleRhinoPanelHost).GUID;

    if (mode == RunMode.Interactive)
    {
      Panels.OpenPanel(panelId);
      return Result.Success;
    }

    bool panelVisible = Panels.IsPanelVisible(panelId);

    string prompt = panelVisible
      ? "SpeckleWebUIWebView2 panel is visible. New value"
      : "SpeckleWebUIWebView2 panel is hidden. New value";

    using GetOption go = new();
    go.SetCommandPrompt(prompt);
    int hideIndex = go.AddOption("Hide");
    int showIndex = go.AddOption("Show");
    int toggleIndex = go.AddOption("Toggle");
    go.Get();

    if (go.CommandResult() != Result.Success)
    {
      return go.CommandResult();
    }

    CommandLineOption option = go.Option();
    if (null == option)
    {
      return Result.Failure;
    }

    int index = option.Index;
    if (index == hideIndex)
    {
      if (panelVisible)
      {
        Panels.ClosePanel(panelId);
      }
    }
    else if (index == showIndex)
    {
      if (!panelVisible)
      {
        Panels.OpenPanel(panelId);
      }
    }
    else if (index == toggleIndex)
    {
      switch (panelVisible)
      {
        case true:
          Panels.ClosePanel(panelId);
          break;
        default:
          Panels.OpenPanel(panelId);
          break;
      }
    }
    return Result.Success;
  }
}
