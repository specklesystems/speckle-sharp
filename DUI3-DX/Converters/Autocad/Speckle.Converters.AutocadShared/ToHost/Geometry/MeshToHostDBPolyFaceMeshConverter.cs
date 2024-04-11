using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Utils;
using System.Linq;
using Speckle.Core.Logging;
using System.Collections.Generic;

namespace Speckle.Converters.Autocad.Geometry;

// POC: there is a transaction error, ingoring for now to not crash acad!
// [NameAndRankValue(nameof(SOG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBMeshToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Mesh, ADB.PolyFaceMesh>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBMeshToHostConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Mesh)target);

  public ADB.PolyFaceMesh RawConvert(SOG.Mesh target)
  {
    target.TriangulateMesh(true);

    // get vertex points
    Point3dCollection vertices = new();
    List<Point3d> points = target.GetPoints().Select(o => _pointConverter.RawConvert(o)).ToList();
    foreach (var point in points)
    {
      vertices.Add(point);
    }

    PolyFaceMesh mesh = new();
    using (Transaction tr = _contextStack.Current.Document.TransactionManager.StartTransaction())
    {
      mesh.SetDatabaseDefaults();

      // append mesh to blocktable record - necessary before adding vertices and faces
      var btr = (BlockTableRecord)
        tr.GetObject(_contextStack.Current.Document.Database.CurrentSpaceId, OpenMode.ForWrite);
      btr.AppendEntity(mesh);
      tr.AddNewlyCreatedDBObject(mesh, true);

      // add polyfacemesh vertices
      for (int i = 0; i < vertices.Count; i++)
      {
        var vertex = new PolyFaceMeshVertex(points[i]);
        if (i < target.colors.Count)
        {
          try
          {
            if (System.Drawing.Color.FromArgb(target.colors[i]) is System.Drawing.Color color)
            {
              vertex.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(color.R, color.G, color.B);
            }
          }
          catch (System.Exception e) when (!e.IsFatal())
          {
            // Couldn't set vertex color, but this should not prevent conversion.
          }
        }

        if (vertex.IsNewObject)
        {
          mesh.AppendVertex(vertex);
          tr.AddNewlyCreatedDBObject(vertex, true);
        }
      }

      // add polyfacemesh faces. vertex index starts at 1 sigh
      int j = 0;
      while (j < target.faces.Count)
      {
        FaceRecord face;
        if (target.faces[j] == 3) // triangle
        {
          face = new FaceRecord(
            (short)(target.faces[j + 1] + 1),
            (short)(target.faces[j + 2] + 1),
            (short)(target.faces[j + 3] + 1),
            0
          );
          j += 4;
        }
        else // quad
        {
          face = new FaceRecord(
            (short)(target.faces[j + 1] + 1),
            (short)(target.faces[j + 2] + 1),
            (short)(target.faces[j + 3] + 1),
            (short)(target.faces[j + 4] + 1)
          );
          j += 5;
        }

        if (face.IsNewObject)
        {
          mesh.AppendFaceRecord(face);
          tr.AddNewlyCreatedDBObject(face, true);
        }
      }

      tr.Commit();
    }

    return mesh;
  }
}
