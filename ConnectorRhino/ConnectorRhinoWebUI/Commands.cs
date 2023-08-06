using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.UI;

namespace ConnectorRhinoWebUI;

// NOTE: we have two commands to test both cefsharp and webview2 in the same host app.

/// <summary>
/// Webview2 Panel
/// </summary>
public class SpeckleWebUiWebView2Command : Command
{
  public override string EnglishName => "SpeckleWebUIWebView2";
  
  public SpeckleWebUiWebView2Command()
  {
    Panels.RegisterPanel(
      ConnectorRhinoWebUiPlugin.Instance,
      typeof(SpeckleWebUiWebView2PanelHost),
      "DUI3WebView",
      System.Drawing.SystemIcons.Information,
      PanelType.System
    );
  }
  
  protected override Result RunCommand(RhinoDoc doc, RunMode mode)
  {
    var panelId = typeof(SpeckleWebUiWebView2PanelHost).GUID;

    if (mode == RunMode.Interactive)
    {
      Panels.OpenPanel(panelId);
      return Result.Success;
    }

    var panelVisible = Panels.IsPanelVisible(panelId);

    var prompt = (panelVisible)
      ? "SpeckleWebUIWebView2 panel is visible. New value"
      : "SpeckleWebUIWebView2 panel is hidden. New value";

    using var go = new GetOption();
    go.SetCommandPrompt(prompt);
    var hideIndex = go.AddOption("Hide");
    var showIndex = go.AddOption("Show");
    var toggleIndex = go.AddOption("Toggle");
    go.Get();

    if (go.CommandResult() != Result.Success)
      return go.CommandResult();

    var option = go.Option();
    if (null == option)
      return Result.Failure;

    var index = option.Index;
    if (index == hideIndex)
    {
      if (panelVisible)
        Panels.ClosePanel(panelId);
    }
    else if (index == showIndex)
    {
      if (!panelVisible)
        Panels.OpenPanel(panelId);
    }
    else if (index == toggleIndex)
    {
      if (panelVisible)
        Panels.ClosePanel(panelId);
      else
        Panels.OpenPanel(panelId);
    }
    return Result.Success;
  }
}

/// <summary>
/// Cefsharp Panel
/// </summary>
public class SpeckleRhinoWebUiCefCommand : Command
{
  public override string EnglishName => "SpeckleRhinoWebUICef";

  public SpeckleRhinoWebUiCefCommand()
  {
    Panels.RegisterPanel(
      ConnectorRhinoWebUiPlugin.Instance,
      typeof(SpeckleWebUiCefPanelHost),
      "DUI3CefSharp",
      System.Drawing.SystemIcons.Information,
      PanelType.System
    );
  }
  

  protected override Result RunCommand(RhinoDoc doc, RunMode mode)
  {
    var panelId = typeof(SpeckleWebUiCefPanelHost).GUID;

    if (mode == RunMode.Interactive)
    {
      Panels.OpenPanel(panelId);
      return Result.Success;
    }

    var panelVisible = Panels.IsPanelVisible(panelId);

    var prompt = (panelVisible)
      ? "SpeckleRhinoWebUICef panel is visible. New value"
      : "SpeckleRhinoWebUICef panel is hidden. New value";

    using var go = new GetOption();
    go.SetCommandPrompt(prompt);
    var hideIndex = go.AddOption("Hide");
    var showIndex = go.AddOption("Show");
    var toggleIndex = go.AddOption("Toggle");
    go.Get();

    if (go.CommandResult() != Result.Success)
      return go.CommandResult();

    var option = go.Option();
    if (null == option)
      return Result.Failure;

    var index = option.Index;
    if (index == hideIndex)
    {
      if (panelVisible)
        Panels.ClosePanel(panelId);
    }
    else if (index == showIndex)
    {
      if (!panelVisible)
        Panels.OpenPanel(panelId);
    }
    else if (index == toggleIndex)
    {
      if (panelVisible)
        Panels.ClosePanel(panelId);
      else
        Panels.OpenPanel(panelId);
    }
    return Result.Success;
  }
}
