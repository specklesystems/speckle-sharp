using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DesktopUI2;
using DesktopUI2.Models.Filters;
using Speckle.Core.Kits;
#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI;

public partial class ConnectorBindingsAutocad : ConnectorBindings
{
  public override List<string> GetObjectsInView() // this returns all visible doc objects.
  {
    var objs = new List<string>();
    using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
    {
      BlockTableRecord modelSpace = Doc.Database.GetModelSpace();
      foreach (ObjectId id in modelSpace)
      {
        var dbObj = tr.GetObject(id, OpenMode.ForRead);
        if (dbObj.Visible())
        {
          objs.Add(dbObj.Handle.ToString());
        }
      }
      tr.Commit();
    }
    return objs;
  }

  public override List<string> GetSelectedObjects()
  {
    var objs = new List<string>();
    if (Doc != null)
    {
      PromptSelectionResult selection = Doc.Editor.SelectImplied();
      if (selection.Status == PromptStatus.OK)
      {
        objs = selection.Value.GetHandles();
      }
    }
    return objs;
  }

  public override List<ISelectionFilter> GetSelectionFilters()
  {
    return new List<ISelectionFilter>()
    {
      new ManualSelectionFilter(),
      new ListSelectionFilter
      {
        Slug = "layer",
        Name = "Layers",
        Icon = "LayersTriple",
        Description = "Selects objects based on their layers.",
        Values = GetLayers()
      },
      new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Selects all document objects."
      }
    };
  }

  public override void SelectClientObjects(List<string> objs, bool deselect = false)
  {
    if (objs is not null && objs.Count > 0)
    {
      Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

      var currentSelection = editor.SelectImplied()?.Value?.GetObjectIds()?.ToList() ?? new List<ObjectId>();
      foreach (var arg in objs)
      {
        if (Utils.GetHandle(arg, out Handle handle))
        {
          if (Doc.Database.TryGetObjectId(handle, out ObjectId id))
          {
            if (deselect)
            {
              currentSelection.Remove(id);
            }
            else
            {
              if (!currentSelection.Contains(id))
              {
                currentSelection.Add(id);
              }
            }
          }
        }
      }

      if (currentSelection.Count == 0)
      {
        editor.SetImpliedSelection(System.Array.Empty<ObjectId>());
      }
      else
      {
        Autodesk.AutoCAD.Internal.Utils.SelectObjects(currentSelection.ToArray());
      }

      Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
    }
  }

  private List<string> GetObjectsFromFilter(ISelectionFilter filter, ISpeckleConverter converter)
  {
    var selection = new List<string>();
    switch (filter.Slug)
    {
      case "manual":
        return filter.Selection;
      case "all":
        return Doc.ConvertibleObjects(converter);
      case "layer":
        foreach (var layerName in filter.Selection)
        {
          TypedValue[] layerType = new TypedValue[1] { new((int)DxfCode.LayerName, layerName) };
          PromptSelectionResult prompt = Doc.Editor.SelectAll(new SelectionFilter(layerType));
          if (prompt.Status == PromptStatus.OK)
          {
            selection.AddRange(prompt.Value.GetHandles());
          }
        }
        return selection;
    }
    return selection;
  }
}
