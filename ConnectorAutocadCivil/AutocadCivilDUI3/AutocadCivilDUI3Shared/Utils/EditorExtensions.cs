using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace AutocadCivilDUI3Shared.Utils;

public static class EditorExtension
{
  public static void Zoom(this Editor editor, Extents3d ext)
  {
    if (editor == null)
      throw new ArgumentNullException(nameof(editor));
    
    using (Transaction tr = editor.Document.TransactionManager.StartTransaction())
    {
      using (ViewTableRecord view = editor.GetCurrentView())
      {
        Matrix3d worldToEye = Matrix3d.WorldToPlane(view.ViewDirection) *
                              Matrix3d.Displacement(Point3d.Origin - view.Target) *
                              Matrix3d.Rotation(view.ViewTwist, view.ViewDirection, view.Target);
        ext.TransformBy(worldToEye);
        view.Width = ext.MaxPoint.X - ext.MinPoint.X;
        view.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
        view.CenterPoint = new Point2d(
          (ext.MaxPoint.X + ext.MinPoint.X) / 2.0,
          (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
        editor.SetCurrentView(view);
        tr.Commit();
      }
    }
  }

  public static void ZoomExtents(this Editor ed)
  {
    Database db = ed.Document.Database;
    db.UpdateExt(false);
    Extents3d ext = (short)Application.GetSystemVariable("cvport") == 1 ?
      new Extents3d(db.Pextmin, db.Pextmax) :
      new Extents3d(db.Extmin, db.Extmax);
    ed.Zoom(ext);
  }
}
