using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.HostApp.Extensions;

public static class EntityExtensions
{
  /// <summary>
  /// Adds an entity to the autocad database model space record
  /// </summary>
  /// <param name="entity">Entity to add into database.</param>
  /// <param name="layer"> Layer to append object.</param>
  public static ObjectId Append(this Entity entity, string? layer = null)
  {
    var db = entity.Database ?? Application.DocumentManager.MdiActiveDocument.Database;
    Transaction tr = db.TransactionManager.TopTransaction;
    if (tr == null)
    {
      return ObjectId.Null;
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
