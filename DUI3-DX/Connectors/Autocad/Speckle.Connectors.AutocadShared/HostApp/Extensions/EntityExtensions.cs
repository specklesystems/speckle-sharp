using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.HostApp.Extensions;

public static class EntityExtensions
{
  /// <summary>
  /// Adds an entity to the autocad database model space record
  /// </summary>
  /// <param name="entity">Entity to add into database.</param>
  /// <param name="layer"> Layer to append object.</param>
  /// <exception cref="InvalidOperationException">Throws when there is no top transaction in the document.</exception>
  public static ObjectId Append(this Entity entity, string? layer = null)
  {
    // POC: Will be addressed to move it into AutocadContext!
    var db = entity.Database ?? Application.DocumentManager.MdiActiveDocument.Database;
    Transaction tr = db.TransactionManager.TopTransaction;
    if (tr == null)
    {
      throw new InvalidOperationException($"Document does not have a top transaction.");
    }

    BlockTableRecord btr = db.GetModelSpace(OpenMode.ForWrite);
    if (entity.IsNewObject)
    {
      if (layer != null)
      {
        entity.Layer = layer;
      }

      var id = btr.AppendEntity(entity);
      tr.AddNewlyCreatedDBObject(entity, true);
      return id;
    }
    else
    {
      if (layer != null)
      {
        entity.Layer = layer;
      }

      return entity.Id;
    }
  }
}
