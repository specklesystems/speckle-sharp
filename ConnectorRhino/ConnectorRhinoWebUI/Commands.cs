using System;
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
    Guid panelId = typeof(SpeckleWebUiWebView2PanelHost).GUID;

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
    Guid panelId = typeof(SpeckleWebUiCefPanelHost).GUID;

    if (mode == RunMode.Interactive)
    {
      Panels.OpenPanel(panelId);
      return Result.Success;
    }

    bool panelVisible = Panels.IsPanelVisible(panelId);

    string prompt = panelVisible
      ? "SpeckleRhinoWebUICef panel is visible. New value"
      : "SpeckleRhinoWebUICef panel is hidden. New value";

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
      if (panelVisible)
      {
        Panels.ClosePanel(panelId);
      }
      else
      {
        Panels.OpenPanel(panelId);
      }
    }
    return Result.Success;
  }
}
