using System;
using System.Collections.Generic;
using System.Linq;
using DesktopUI2;
using DesktopUI2.Models.Filters;
using Rhino.DocObjects;
using Speckle.Core.Kits;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  public override List<string> GetSelectedObjects()
  {
    var objs = new List<string>();
    var Converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);

    if (Converter == null || Doc == null)
      return objs;

    var selected = Doc.Objects.GetSelectedObjects(true, false).ToList();
    if (selected.Count == 0)
      return objs;
    var supportedObjs = selected.Where(o => Converter.CanConvertToSpeckle(o))?.ToList();
    var unsupportedObjs = selected.Where(o => Converter.CanConvertToSpeckle(o) == false)?.ToList();

    // handle any unsupported objects and modify doc selection if so
    if (unsupportedObjs.Count > 0)
    {
      LogUnsupportedObjects(unsupportedObjs, Converter);
      Doc.Objects.UnselectAll(false);
      supportedObjs.ForEach(o => o.Select(true, true));
    }

    return supportedObjs.Select(o => o.Id.ToString())?.ToList();
  }

  public override List<ISelectionFilter> GetSelectionFilters()
  {
    var layers = Doc.Layers.ToList().Where(layer => !layer.IsDeleted).Select(layer => layer.FullPath).ToList();
    var projectInfo = new List<string> { "Named Views", "Standard Views", "Layers" };

    return new List<ISelectionFilter>
    {
      new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Selects all document objects and project info."
      },
      new ListSelectionFilter
      {
        Slug = "layer",
        Name = "Layers",
        Icon = "LayersTriple",
        Description = "Selects objects based on their layers.",
        Values = layers
      },
      new ListSelectionFilter
      {
        Slug = "project-info",
        Name = "Project Information",
        Icon = "Information",
        Values = projectInfo,
        Description = "Adds the selected project information as views to the stream"
      },
      new ManualSelectionFilter()
    };
  }

  public override void SelectClientObjects(List<string> objs, bool deselect = false)
  {
    var isPreview = PreviewConduit != null && PreviewConduit.Enabled ? true : false;

    foreach (var id in objs)
    {
      RhinoObject obj = null;
      try
      {
        obj = Doc.Objects.FindId(new Guid(id)); // this is a rhinoobj
      }
      catch
      {
        continue; // this was a named view!
      }

      if (obj != null)
      {
        if (deselect)
          obj.Select(false, true, false, true, true, true);
        else
          obj.Select(true, true, true, true, true, true);
      }
      else if (isPreview)
      {
        PreviewConduit.Enabled = false;
        PreviewConduit.SelectPreviewObject(id, deselect);
        PreviewConduit.Enabled = true;
      }
    }

    Doc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();
    Doc.Views.Redraw();
  }

  private List<string> GetObjectsFromFilter(ISelectionFilter filter)
  {
    var objs = new List<string>();

    switch (filter.Slug)
    {
      case "manual":
        return filter.Selection;
      case "all":
        objs.AddRange(Doc.Layers.Select(o => o.Id.ToString()));
        objs.AddRange(Doc.Objects.Where(obj => obj.Visible).Select(obj => obj.Id.ToString()));
        objs.AddRange(Doc.StandardViews());
        objs.AddRange(Doc.NamedViews());
        break;
      case "layer":
        foreach (var layerPath in filter.Selection)
        {
          Layer layer = Doc.GetLayer(layerPath);
          if (layer != null && layer.IsVisible)
          {
            var layerObjs = Doc.Objects.FindByLayer(layer)?.Select(o => o.Id.ToString());
            if (layerObjs != null)
              objs.AddRange(layerObjs);
          }
        }
        break;
      case "project-info":
        if (filter.Selection.Contains("Standard Views"))
          objs.AddRange(Doc.StandardViews());
        if (filter.Selection.Contains("Named Views"))
          objs.AddRange(Doc.NamedViews());
        if (filter.Selection.Contains("Layers"))
          objs.AddRange(Doc.Layers.Select(o => o.Id.ToString()));
        break;
    }

    return objs;
  }
}
