﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;
using ComBridge = Autodesk.Navisworks.Api.ComApi.ComApiBridge;
using Plane = Objects.Geometry.Plane;
using Vector = Objects.Geometry.Vector; //

namespace Objects.Converter.Navisworks
{
  public class PrimitiveProcessor : InwSimplePrimitivesCB
  {
    public List<double> Coords { get; set; }
    public List<int> Faces { get; set; }
    public List<TriangleD> Triangles { get; set; }
    public List<LineD> Lines { get; set; }
    public List<PointD> Points { get; set; }
    public double[] LocalToWorldTransformation { get; set; }
    public bool ElevationMode { get; set; }

    public PrimitiveProcessor()
    {
      Coords = new List<double>();
      Faces = new List<int>();
      Triangles = new List<TriangleD>();
      Lines = new List<LineD>();
      Points = new List<PointD>();
    }

    public PrimitiveProcessor(bool elevationMode) : this()
    {
      ElevationMode = elevationMode;
    }

    public void Line(InwSimpleVertex v1, InwSimpleVertex v2)
    {
      var vD1 = SetElevationModeVector(ApplyTransformation(VectorFromVertex(v1), LocalToWorldTransformation),
        ElevationMode);
      var vD2 = SetElevationModeVector(ApplyTransformation(VectorFromVertex(v2), LocalToWorldTransformation),
        ElevationMode);

      var line = new LineD();
      try
      {
        line = new LineD(vD1, vD2);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }

      Lines.Add(line);
    }

    public void Point(InwSimpleVertex v1)
    {
      var vD1 = SetElevationModeVector(ApplyTransformation(VectorFromVertex(v1), LocalToWorldTransformation),
        ElevationMode);

      Points.Add(new PointD(vD1));
    }

    public void SnapPoint(InwSimpleVertex v1)
    {
      // Needed for Splines
    }

    public void Triangle(InwSimpleVertex v1, InwSimpleVertex v2, InwSimpleVertex v3)
    {
      var vD1 = SetElevationModeVector(ApplyTransformation(VectorFromVertex(v1), LocalToWorldTransformation),
        ElevationMode);
      var vD2 = SetElevationModeVector(ApplyTransformation(VectorFromVertex(v2), LocalToWorldTransformation),
        ElevationMode);
      var vD3 = SetElevationModeVector(ApplyTransformation(VectorFromVertex(v3), LocalToWorldTransformation),
        ElevationMode);

      var indexPointer = Faces.Count;
      Faces.Add(3);
      Coords.AddRange(new[] { vD1.X, vD1.Y, vD1.Z, vD2.X, vD2.Y, vD2.Z, vD3.X, vD3.Y, vD3.Z });
      Faces.AddRange(new[] { indexPointer + 0, indexPointer + 1, indexPointer + 2 });

      Triangles.Add(new TriangleD(vD1, vD2, vD3));
    }

    private static Vector3D SetElevationModeVector(Vector3D v, bool elevationMode) =>
      elevationMode ? v : new Vector3D(v.X, -v.Z, v.Y);

    private static Vector3D ApplyTransformation(Vector3 vector3, IReadOnlyList<double> matrix)
    {
      double t1 = matrix[3] * vector3.X + matrix[7] * vector3.Y + matrix[11] * vector3.Z + matrix[15];
      var vectorDoubleX = (matrix[0] * vector3.X + matrix[4] * vector3.Y + matrix[8] * vector3.Z + matrix[12]) / t1;
      var vectorDoubleY = (matrix[1] * vector3.X + matrix[5] * vector3.Y + matrix[9] * vector3.Z + matrix[13]) / t1;
      var vectorDoubleZ = (matrix[2] * vector3.X + matrix[6] * vector3.Y + matrix[10] * vector3.Z + matrix[14]) / t1;

      return new Vector3D(vectorDoubleX, vectorDoubleY, vectorDoubleZ);
    }

    private static Vector3 VectorFromVertex(InwSimpleVertex v)
    {
      Array arrayV = (Array)v.coord;
      return new Vector3(
        (float)arrayV.GetValue(1),
        (float)arrayV.GetValue(2),
        (float)arrayV.GetValue(3)
      );
    }
  }


  public class NavisworksGeometry
  {
    public InwOpSelection Selection { get; set; }
    public ModelItem ModelItem { get; set; }
    public Stack<InwOaFragment3> ModelFragments { get; set; }
    public Base Geometry { get; internal set; }
    public Base Base { get; internal set; }

    public bool ElevationMode { get; set; } = false;

    public NavisworksGeometry(ModelItem modelItem)
    {
      ModelItem = modelItem;

      // Add conversion geometry to oModelColl Property
      ModelItemCollection modelItemCollection = new ModelItemCollection { modelItem };

      //convert to COM selection
      Selection = ComBridge.ToInwOpSelection(modelItemCollection);
    }

    public List<PrimitiveProcessor> GetUniqueGeometryFragments()
    {
      var processors = new List<PrimitiveProcessor>();

      foreach (InwOaPath path in Selection.Paths())
      {
        var processor = new PrimitiveProcessor(ElevationMode);

        foreach (var fragment in ModelFragments)
        {
          if (!IsSameFragmentPath(((Array)fragment.path.ArrayData).ToArray<int>(),
                ((Array)path.ArrayData).ToArray<int>())) continue;

          var localToWorldTransform = (InwLTransform3f3)fragment.GetLocalToWorldMatrix();
          processor.LocalToWorldTransformation = ConvertArrayToDouble((Array)localToWorldTransform.Matrix);
          fragment.GenerateSimplePrimitives(nwEVertexProperty.eNORMAL, processor);
        }

        processors.Add(processor);
      }

      return processors;
    }

    private static bool IsSameFragmentPath(Array a1, Array a2) =>
      a1.Length == a2.Length && a1.Cast<int>().SequenceEqual(a2.Cast<int>());

    public static double[] ConvertArrayToDouble(Array arr)
    {
      if (arr.Rank != 1)
      {
        throw new ArgumentException();
      }

      var doubleArray = new double[arr.GetLength(0)];
      for (var ix = arr.GetLowerBound(0); ix <= arr.GetUpperBound(0); ++ix)
      {
        doubleArray[ix - arr.GetLowerBound(0)] = (double)arr.GetValue(ix);
      }

      return doubleArray;
    }
  }

  /// <summary>
  /// A Triangle where all vertices are in turn stored with double values as opposed to floats
  /// </summary>
  public class TriangleD
  {
    public Vector3D Vertex1 { get; set; }
    public Vector3D Vertex2 { get; set; }
    public Vector3D Vertex3 { get; set; }

    public TriangleD(Vector3D v1, Vector3D v2, Vector3D v3)
    {
      Vertex1 = v1;
      Vertex2 = v2;
      Vertex3 = v3;
    }
  }

  /// <summary>
  /// A Line where each end point vertex is in turn stored with double values as opposed to floats
  /// </summary>
  public class LineD
  {
    public Vector3D Vertex1 { get; set; }
    public Vector3D Vertex2 { get; set; }


    public LineD()
    {
      Vertex1 = new Vector3D();
      Vertex2 = new Vector3D();
    }

    public LineD(Vector3D v1, Vector3D v2)
    {
      Vertex1 = v1;
      Vertex2 = v2;
    }
  }

  /// <summary>
  /// A Line where each end point vertex is in turn stored with double values as opposed to floats
  /// </summary>
  public class PointD
  {
    public Vector3D Vertex1 { get; set; }

    public PointD(Vector3D vertex1)
    {
      Vertex1 = vertex1;
    }
  }


  public partial class ConverterNavisworks
  {
    public static Box BoxToSpeckle(BoundingBox3D boundingBox3D)
    {
      Units source = Application.ActiveDocument.Units;
      Units target = Units.Meters;
      double scale = UnitConversion.ScaleFactor(source, target);

      Point3D min = boundingBox3D.Min;
      Point3D max = boundingBox3D.Max;

      var basePlane = new Plane
      {
        units = source.ToString(),
        origin = new Point(0, 0),
        xdir = new Vector(1, 0),
        ydir = new Vector(0, 1),
        normal = new Vector(0, 0, 1)
      };

      Box boundingBox = new Box
      {
        units = source.ToString(),
        basePlane = basePlane,
        xSize = new Interval(min.X * scale, max.X * scale),
        ySize = new Interval(min.Y * scale, max.Y * scale),
        zSize = new Interval(min.Z * scale, max.Z * scale)
      };

      return boundingBox;
    }

    public static Vector3D TransformVector3D { get; set; }
    public Vector SettingOutPoint { get; set; }
    public Vector TransformVector { get; set; }
    public BoundingBox3D ModelBoundingBox { get; set; }

    /// <summary>
    /// ElevationMode is the indicator that the model is being handled as an XY ground plane
    /// with Z as elevation height.
    /// This is distinct from the typical "handedness" of 3D models.
    /// </summary>
    public static bool ElevationMode { get; set; }

    public void SetModelOrientationMode()
    {
      Vector3D elevationModeUpVector = new Vector3D(0, 0, 1);
      Vector3D elevationModeRightVector = new Vector3D(1, 0, 0);

      bool upMatch = VectorMatch(Doc.UpVector, elevationModeUpVector);
      bool rightMatch = VectorMatch(Doc.RightVector, elevationModeRightVector);

      // TODO: do both need to match or would UP be enough?
      ElevationMode = upMatch && rightMatch;
    }

    /// <summary>
    /// Compares two vectors as identical with an optional tolerance.
    /// </summary>
    /// <param name="vectorA">The first comparison vector</param>
    /// <param name="vectorB">The second comparison vector</param>
    /// <param name="tolerance">Default value of 1e-9</param>
    /// <returns>Boolean value indicating match success</returns>
    private static bool VectorMatch(Vector3D vectorA, Vector3D vectorB, double tolerance = 1e-9) =>
      Math.Abs(vectorA.X - vectorB.X) < tolerance &&
      Math.Abs(vectorA.Y - vectorB.Y) < tolerance &&
      Math.Abs(vectorA.Z - vectorB.Z) < tolerance;


    public static void PopulateModelFragments(NavisworksGeometry geometry)
    {
      geometry.ModelFragments = new Stack<InwOaFragment3>();

      var paths = geometry.Selection.Paths();

      foreach (InwOaPath path in paths)
      {
        var fragments = path.Fragments();
        foreach (InwOaFragment3 fragment in fragments)
        {
          int[] a1 = ((Array)fragment.path.ArrayData).ToArray<int>();
          int[] a2 = ((Array)path.ArrayData).ToArray<int>();
          bool isSame = !(a1.Length != a2.Length || !a1.SequenceEqual(a2));

          if (isSame)
          {
            geometry.ModelFragments.Push(fragment);
          }

          GC.KeepAlive(fragments);
        }

        GC.KeepAlive(paths);
      }
    }


    public static List<Base> TranslateFragmentGeometry(NavisworksGeometry navisworksGeometry)
    {
      var callbackListeners = navisworksGeometry.GetUniqueGeometryFragments();

      var baseGeometries = new List<Base>();

      var move = TransformVector3D;

      foreach (var callback in callbackListeners)
      {
        var triangles = callback.Triangles;
        var lines = callback.Lines;

        // TODO: Additional Primitive Types
        //List<NavisworksDoublePoint> Points = callback.Points;


        var source = Application.ActiveDocument.Units;
        var scale = UnitConversion.ScaleFactor(source, Units.Meters);

        var vertices = new List<double>();

        if (triangles != null)
        {
          var faces = new List<int>();
          var triangleCount = triangles.Count;
          if (triangleCount > 0)
          {
            for (var t = 0; t < triangleCount; t += 1)
            {
              // Apply the bounding box move.
              vertices.AddRange(MoveAndScaleVertices(triangles[t].Vertex1, move, scale));
              vertices.AddRange(MoveAndScaleVertices(triangles[t].Vertex2, move, scale));
              vertices.AddRange(MoveAndScaleVertices(triangles[t].Vertex3, move, scale));

              faces.AddRange(new[] { 0, t * 3, t * 3 + 1, t * 3 + 2 });
            }

            var baseMesh = new Mesh(vertices, faces)
            {
              ["renderMaterial"] = TranslateMaterial(navisworksGeometry.ModelItem)
            };
            baseGeometries.Add(baseMesh);
          }
        }

        if (lines != null)
        {
          var lineCount = lines.Count;
          if (lineCount <= 0) continue;
          for (var i = 0; i < lines.Count; i++)
          {
            var lineD = lines[i];

            var verticesA = MoveAndScaleVertices(lineD.Vertex1, move, scale).ToArray();
            var verticesB = MoveAndScaleVertices(lineD.Vertex2, move, scale).ToArray();

            var start = new Point(verticesA[0], verticesA[1], verticesA[2]);
            var end = new Point(verticesB[0], verticesB[1], verticesB[2]);

            var baseLine = new Line(start, end)
            {
              ["renderMaterial"] = TranslateMaterial(navisworksGeometry.ModelItem)
            };

            baseGeometries.Add(baseLine);
          }
        }
      }

      return baseGeometries;
    }

    private void SetModelBoundingBox()
    {
      ModelBoundingBox = Doc.GetBoundingBox(false);
    }

    private void SetTransformVector3D()
    {
      if (TransformVector3D != null) return;

      Vector3D transform;

      switch (ModelTransform)
      {
        case Transforms.ProjectBasePoint:
          Units source = Application.ActiveDocument.Units;

          // Coordinate Units are likely to be set to match the HUD readout which is
          // different to the internal units of constituent models.
          double scale = UnitConversion.ScaleFactor(CoordinateUnits, source);

          transform = new Vector3D(-ProjectBasePoint.X * scale, -ProjectBasePoint.Y * scale, 0);
          break;
        case Transforms.BoundingBox:
          transform = new Vector3D(-ModelBoundingBox.Center.X, -ModelBoundingBox.Center.Y, 0);
          break;
        case Transforms.Default:
        default:
          transform = new Vector3D(0, 0, 0);
          break;
      }

      TransformVector3D = transform;
    }

    private static IEnumerable<double> MoveAndScaleVertices(Vector3D vertex1, Vector3D move, double scale)
    {
      return new List<double>
      {
        (vertex1.X + move.X) * scale, (vertex1.Y + move.Y) * scale, (vertex1.Z + move.Z) * scale
      };
    }
  }
}
