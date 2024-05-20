using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(ADB.SubDMesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBSubDMeshToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBSubDMeshToSpeckleConverter(
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.SubDMesh)target);

  public SOG.Mesh RawConvert(ADB.SubDMesh target)
  {
    //vertices
    var vertices = new List<double>(target.Vertices.Count * 3);
    foreach (AG.Point3d vert in target.Vertices)
    {
      vertices.AddRange(_pointConverter.RawConvert(vert).ToList());
    }

    // faces
    var faces = new List<int>();
    int[] faceArr = target.FaceArray.ToArray(); // contains vertex indices
    int edgeCount = 0;
    for (int i = 0; i < faceArr.Length; i = i + edgeCount + 1)
    {
      List<int> faceVertices = new();
      edgeCount = faceArr[i];
      for (int j = i + 1; j <= i + edgeCount; j++)
      {
        faceVertices.Add(faceArr[j]);
      }

      if (edgeCount == 4) // quad face
      {
        faces.AddRange(new List<int> { 4, faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] });
      }
      else // triangle face
      {
        faces.AddRange(new List<int> { 3, faceVertices[0], faceVertices[1], faceVertices[2] });
      }
    }

    // colors
    var colors = target.VertexColorArray
      .Select(
        o =>
          System.Drawing.Color
            .FromArgb(System.Convert.ToInt32(o.Red), System.Convert.ToInt32(o.Green), System.Convert.ToInt32(o.Blue))
            .ToArgb()
      )
      .ToList();

    // bbox
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Mesh speckleMesh = new(vertices, faces, colors, null, _contextStack.Current.SpeckleUnits) { bbox = bbox };

    return speckleMesh;
  }
}
