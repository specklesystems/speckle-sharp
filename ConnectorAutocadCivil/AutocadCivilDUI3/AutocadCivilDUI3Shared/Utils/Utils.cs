using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Speckle.Core.Kits;

namespace AutocadCivilDUI3Shared.Utils;

public static class Utils
{
#if AUTOCAD2021DUI3
  public static readonly string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2021);
  public static readonly string AppName = HostApplications.AutoCAD.Name;
  public static readonly string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2022DUI3
  public static readonly string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2022);
  public static readonly string AppName = HostApplications.AutoCAD.Name;
  public static readonly string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2023DUI3
  public static readonly string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2023);
  public static readonly string AppName = HostApplications.AutoCAD.Name;
  public static readonly string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2024DUI3
  public static readonly string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2024);
  public static readonly string AppName = HostApplications.AutoCAD.Name;
  public static readonly string Slug = HostApplications.AutoCAD.Slug;
#endif
  public static readonly string InvalidChars = @"<>/\:;""?*|=,‘";

  public static string RemoveInvalidChars(string str)
  {
    foreach (char c in InvalidChars)
    {
      str = str.Replace(c.ToString(), string.Empty);
    }

    return str;
  }

  /// <summary>
  /// Adds an entity to the autocad database model space record
  /// </summary>
  /// <param name="entity"></param>
  /// <param name="tr"></param>
  public static ObjectId Append(this Entity entity, string layer = null)
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

  /// <summary>
  /// Gets the document model space
  /// </summary>
  /// <param name="db"></param>
  /// <param name="mode"></param>
  /// <returns></returns>
  public static BlockTableRecord GetModelSpace(this Database db, OpenMode mode = OpenMode.ForRead)
  {
    return (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(mode);
  }
}
