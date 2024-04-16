using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(ADB.PolyFaceMesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBMeshToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBMeshToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
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
    using (Transaction tr = _contextStack.Current.Document.Database.TransactionManager.StartTransaction())
    {
      foreach (ObjectId id in target)
      {
        DBObject obj = tr.GetObject(id, OpenMode.ForRead);
        switch (obj)
        {
          case PolyFaceMeshVertex o:
            dbVertices.Add(o.Position);
            colors.Add(o.Color.ColorValue.ToArgb());
            break;
          case FaceRecord o:
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

[NameAndRankValue(nameof(ADB.SubDMesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBSubDMeshToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBSubDMeshToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
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
    foreach (Point3d vert in target.Vertices)
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
