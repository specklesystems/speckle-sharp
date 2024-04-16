using Autodesk.AutoCAD.DatabaseServices;
using System.Collections;
using System.Collections.Generic;

namespace Speckle.Converters.Autocad.Extensions;

public static class EntityExtensions
{
  /// <summary>
  /// Worker for enumeration of IEnumerable entities that return differently typed subentities depending on the database-residency of the entity.
  /// </summary>
  /// <remarks>
  /// Database-resident entities which are enumerable will return ObjectIds of subentities when enumerated, and requires a transaction.
  /// Non database-resident entities (eg polyline2ds from exploded blocks) will return the typed subentities, are also not database-resident.
  /// Can be used for retrieving <see cref="AttributeCollection"/> in <see cref="BlockReference"/>,
  /// <see cref="PolyFaceMeshVertex"/> in <see cref="PolyFaceMesh"/> and <see cref="PolygonMeshVertex"/> in <see cref="PolygonMesh"/>,
  /// <see cref="Vertex2d"/> in <see cref="Polyline2d"/> and <see cref="PolylineVertex3d"/> in <see cref="Polyline3d"/>.
  /// </remarks>
  /// <exception cref="System.ArgumentException">Thrown when source arg is null and owner entity was not of type IEnumerable.</exception>
  /// <exception cref="System.InvalidOperationException">Thrown when input transaction is null and owner database has no active top transaction.</exception>
  public static IEnumerable<T> GetSubEntities<T>(
    this Entity owner,
    OpenMode mode = OpenMode.ForRead,
    Transaction? trans = null,
    IEnumerable? source = null,
    bool forceOpenOnLockedLayer = false
  )
    where T : Entity
  {
    if ((source ?? owner as IEnumerable) is not IEnumerable enumerable)
    {
      throw new System.ArgumentException("IEnumerable source is null and owner Entity is not of type IEnumerable.");
    }

    if (owner.Database is Database db)
    {
      if ((trans ?? db.TransactionManager.TopTransaction) is not Transaction tr)
      {
        throw new System.InvalidOperationException(
          "Transaction is null and the owner database has no active top transaction."
        );
      }

      foreach (ObjectId id in enumerable)
      {
        yield return (T)tr.GetObject(id, mode, false, forceOpenOnLockedLayer);
      }
    }
    else
    {
      foreach (T entity in enumerable)
      {
        yield return entity;
      }
    }
  }
}
