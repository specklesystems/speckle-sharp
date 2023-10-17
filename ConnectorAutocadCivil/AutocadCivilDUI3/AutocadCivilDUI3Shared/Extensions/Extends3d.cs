using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace AutocadCivilDUI3Shared.Extensions;

public static class Extends3dExtensions
{
  public static Extents3d FromObjectIds(Editor editor, IReadOnlyCollection<ObjectId> objectIds)
  {
    Transaction tr = editor.Document.Database.TransactionManager.StartTransaction();
    
    PromptSelectionResult selectionResult = editor.SelectImplied();
    
    if (selectionResult.Status == PromptStatus.OK)
    {
      SelectionSet selectionSet = selectionResult.Value;

      if (selectionSet.Count > 0)
      {
        // Create a bounding box to include all selected objects
        Extents3d selectedExtents = new();

        foreach (ObjectId objectId in selectionSet.GetObjectIds())
        {
          Entity entity = tr.GetObject(objectId, OpenMode.ForRead) as Entity;
          if (entity != null)
          {
            selectedExtents.AddExtents(entity.GeometricExtents);
          }
        }
        tr.Commit();
        return selectedExtents;
      }
    }
    
    tr.Commit();
    return new Extents3d();
  }
}
