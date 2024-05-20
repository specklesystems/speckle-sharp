using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Utils;
using Speckle.Core.Logging;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(SOG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Mesh, ADB.PolyFaceMesh>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public MeshToHostConverter(
    ITypedConverter<SOG.Point, AG.Point3d> pointConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Mesh)target);

  /// <remarks>
  /// Mesh conversion requires transaction since it's vertices needed to be added into database in advance..
  /// </remarks>
  public ADB.PolyFaceMesh Convert(SOG.Mesh target)
  {
    target.TriangulateMesh(true);

    // get vertex points
    using AG.Point3dCollection vertices = new();
    List<AG.Point3d> points = target.GetPoints().Select(o => _pointConverter.Convert(o)).ToList();
    foreach (var point in points)
    {
      vertices.Add(point);
    }

    ADB.PolyFaceMesh mesh = new();

    ADB.Transaction tr = _contextStack.Current.Document.TransactionManager.TopTransaction;

    mesh.SetDatabaseDefaults();

    // append mesh to blocktable record - necessary before adding vertices and faces
    var btr = (ADB.BlockTableRecord)
      tr.GetObject(_contextStack.Current.Document.Database.CurrentSpaceId, ADB.OpenMode.ForWrite);
    btr.AppendEntity(mesh);
    tr.AddNewlyCreatedDBObject(mesh, true);

    // add polyfacemesh vertices
    for (int i = 0; i < vertices.Count; i++)
    {
      var vertex = new ADB.PolyFaceMeshVertex(points[i]);
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
          // POC: should we warn user?
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
      ADB.FaceRecord face;
      if (target.faces[j] == 3) // triangle
      {
        face = new ADB.FaceRecord(
          (short)(target.faces[j + 1] + 1),
          (short)(target.faces[j + 2] + 1),
          (short)(target.faces[j + 3] + 1),
          0
        );
        j += 4;
      }
      else // quad
      {
        face = new ADB.FaceRecord(
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

    return mesh;
  }
}
