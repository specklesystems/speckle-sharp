using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="ADB.PolyFaceMesh"/> class converter. Converts to <see cref="SOG.Mesh"/>.
/// </summary>
/// <remarks>
/// The IHostObjectToSpeckleConversion inheritance should only expect database-resident <see cref="ADB.PolyFaceMesh"/> objects. IRawConversion inheritance can expect non database-resident objects, when generated from other converters.
/// </remarks>
[NameAndRankValue(nameof(ADB.PolyFaceMesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBPolyfaceMeshToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBPolyfaceMeshToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.PolyFaceMesh)target);

  public SOG.Mesh RawConvert(ADB.PolyFaceMesh target)
  {
    List<Point3d> dbVertices = new();
    List<int> faces = new();
    List<int> faceVisibility = new();
    List<int> colors = new();
    using (ADB.Transaction tr = _contextStack.Current.Document.Database.TransactionManager.StartTransaction())
    {
      foreach (ADB.ObjectId id in target)
      {
        ADB.DBObject obj = tr.GetObject(id, ADB.OpenMode.ForRead);
        switch (obj)
        {
          case ADB.PolyFaceMeshVertex o:
            dbVertices.Add(o.Position);
            colors.Add(o.Color.ColorValue.ToArgb());
            break;
          case ADB.FaceRecord o:
            List<int> indices = new();
            List<int> hidden = new();
            for (short i = 0; i < 4; i++)
            {
              short index = o.GetVertexAt(i);
              if (index == 0)
              {
                continue;
              }

              // vertices are 1 indexed, and can be negative (hidden)
              int adjustedIndex = index > 0 ? index - 1 : Math.Abs(index) - 1;
              indices.Add(adjustedIndex);

              // 0 indicates hidden vertex on the face: 1 indicates a visible vertex
              hidden.Add(index < 0 ? 0 : 1);
            }

            if (indices.Count == 4)
            {
              faces.AddRange(new List<int> { 4, indices[0], indices[1], indices[2], indices[3] });
              faceVisibility.AddRange(new List<int> { 4, hidden[0], hidden[1], hidden[2], hidden[3] });
            }
            else
            {
              faces.AddRange(new List<int> { 3, indices[0], indices[1], indices[2] });
              faceVisibility.AddRange(new List<int> { 3, hidden[0], hidden[1], hidden[2] });
            }

            break;
        }
      }
      tr.Commit();
    }

    List<double> vertices = new(dbVertices.Count * 3);
    foreach (Point3d vert in dbVertices)
    {
      vertices.AddRange(_pointConverter.RawConvert(vert).ToList());
    }

    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Mesh speckleMesh =
      new(vertices, faces, colors, null, _contextStack.Current.SpeckleUnits)
      {
        bbox = bbox,
        ["faceVisibility"] = faceVisibility
      };

    return speckleMesh;
  }
}
