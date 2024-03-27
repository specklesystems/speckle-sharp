using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Speckle.Connectors.Autocad.HostApp.Extensions;

public static class EditorExtensions
{
  public static void Zoom(this Editor editor, Extents3d ext)
  {
    if (editor == null)
    {
      throw new ArgumentNullException(nameof(editor));
    }

    using ViewTableRecord view = editor.GetCurrentView();
    Matrix3d worldToEye =
      Matrix3d.WorldToPlane(view.ViewDirection)
      * Matrix3d.Displacement(Point3d.Origin - view.Target)
      * Matrix3d.Rotation(view.ViewTwist, view.ViewDirection, view.Target);
    ext.TransformBy(worldToEye);
    view.Width = ext.MaxPoint.X - ext.MinPoint.X;
    view.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
    view.CenterPoint = new Point2d((ext.MaxPoint.X + ext.MinPoint.X) / 2.0, (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
    editor.SetCurrentView(view);
  }
}
