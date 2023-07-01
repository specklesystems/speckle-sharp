using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;

namespace ConnectorRhinoWebUI
{
  [Guid("55B9125D-E8CA-4F65-B016-60DA932AB694")]
  public class SpeckleWebUIPanelHost : RhinoWindows.Controls.WpfElementHost
  {
    public SpeckleWebUIPanelHost(uint docSn)
      : base(new SpeckleWebUIPanel(), null)
    {
    }
  }

  public class SpeckleRhinoWebUI : Command
  {
    public SpeckleRhinoWebUI()
    {
      Instance = this;
      Panels.RegisterPanel(
        ConnectorRhinoWebUIPlugin.Instance,
        typeof(SpeckleWebUIPanelHost),
        "SpeckleWebUI",
        System.Drawing.SystemIcons.Information,
        PanelType.System
      );
    }

    public static SpeckleRhinoWebUI Instance { get; private set; }

    public override string EnglishName => "SpeckleRhinoWebUI";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      //RhinoApp.WriteLine("The {0} command is under construction.", EnglishName);
      //return Result.Success;

      var panel_id = typeof(SpeckleWebUIPanelHost).GUID;

      if (mode == RunMode.Interactive)
      {
        Panels.OpenPanel(panel_id);
        return Result.Success;
      }

      var panel_visible = Panels.IsPanelVisible(panel_id);

      var prompt = (panel_visible)
        ? "SpeckleRhinoWebUI panel is visible. New value"
        : "SpeckleRhinoWebUI panel is hidden. New value";

      using var go = new GetOption();
      go.SetCommandPrompt(prompt);
      var hide_index = go.AddOption("Hide");
      var show_index = go.AddOption("Show");
      var toggle_index = go.AddOption("Toggle");
      go.Get();

      if (go.CommandResult() != Result.Success)
        return go.CommandResult();

      var option = go.Option();
      if (null == option)
        return Result.Failure;

      var index = option.Index;
      if (index == hide_index)
      {
        if (panel_visible)
          Panels.ClosePanel(panel_id);
      }
      else if (index == show_index)
      {
        if (!panel_visible)
          Panels.OpenPanel(panel_id);
      }
      else if (index == toggle_index)
      {
        if (panel_visible)
          Panels.ClosePanel(panel_id);
        else
          Panels.OpenPanel(panel_id);
      }
      return Result.Success;
    }
  }
}
