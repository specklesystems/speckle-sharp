using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;
using ComBridge = Autodesk.Navisworks.Api.ComApi.ComApiBridge; //

namespace Objects.Converter.Navisworks
{
  public class CallbackGeomListener : InwSimplePrimitivesCB
  {
    public List<double> Coords { get; set; }

    public List<int> Faces { get; set; }
    public List<NavisworksDoubleTriangle> Triangles { get; set; }
    public double[] Matrix { get; set; }

    public CallbackGeomListener()
    {
      Coords = new List<double>();
      Faces = new List<int>();
      Triangles = new List<NavisworksDoubleTriangle>();
    }

    public void Line(InwSimpleVertex v1, InwSimpleVertex v2)
    {
    }

    public void Point(InwSimpleVertex v1)
    {
    }

    public void SnapPoint(InwSimpleVertex v1)
    {
    }

    public void Triangle(InwSimpleVertex v1, InwSimpleVertex v2, InwSimpleVertex v3)
    {
      int indexPointer = Faces.Count;

      Array arrayV1 = (Array)v1.coord;
      double v1X = (float)arrayV1.GetValue(1);
      double v1Y = (float)arrayV1.GetValue(2);
      double v1Z = (float)arrayV1.GetValue(3);

      Array arrayV2 = (Array)v2.coord;
      double v2X = (float)arrayV2.GetValue(1);
      double v2Y = (float)arrayV2.GetValue(2);
      double v2Z = (float)arrayV2.GetValue(3);

      Array arrayV3 = (Array)v3.coord;
      double v3X = (float)arrayV3.GetValue(1);
      double v3Y = (float)arrayV3.GetValue(2);
      double v3Z = (float)arrayV3.GetValue(3);

      //Matrix transformation
      double t1 = Matrix[3] * v1X + Matrix[7] * v1Y + Matrix[11] * v1Z + Matrix[15];
      double vv1X = (Matrix[0] * v1X + Matrix[4] * v1Y + Matrix[8] * v1Z + Matrix[12]) / t1;
      double vv1Y = (Matrix[1] * v1X + Matrix[5] * v1Y + Matrix[9] * v1Z + Matrix[13]) / t1;
      double vv1Z = (Matrix[2] * v1X + Matrix[6] * v1Y + Matrix[10] * v1Z + Matrix[14]) / t1;

      double t2 = Matrix[3] * v2X + Matrix[7] * v2Y + Matrix[11] * v2Z + Matrix[15];
      double vv2X = (Matrix[0] * v2X + Matrix[4] * v2Y + Matrix[8] * v2Z + Matrix[12]) / t2;
      double vv2Y = (Matrix[1] * v2X + Matrix[5] * v2Y + Matrix[9] * v2Z + Matrix[13]) / t2;
      double vv2Z = (Matrix[2] * v2X + Matrix[6] * v2Y + Matrix[10] * v2Z + Matrix[14]) / t2;

      double t3 = Matrix[3] * v3X + Matrix[7] * v3Y + Matrix[11] * v3Z + Matrix[15];
      double vv3X = (Matrix[0] * v3X + Matrix[4] * v3Y + Matrix[8] * v3Z + Matrix[12]) / t3;
      double vv3Y = (Matrix[1] * v3X + Matrix[5] * v3Y + Matrix[9] * v3Z + Matrix[13]) / t3;
      double vv3Z = (Matrix[2] * v3X + Matrix[6] * v3Y + Matrix[10] * v3Z + Matrix[14]) / t3;

      Faces.Add(3); //TRIANGLE FLAG

      // Triangle by 3 Vertices
      Coords.Add(vv1X);
      Coords.Add(vv1Y);
      Coords.Add(vv1Z);
      Faces.Add(indexPointer + 0);

      Coords.Add(vv2X);
      Coords.Add(vv2Y);
      Coords.Add(vv2Z);
      Faces.Add(indexPointer + 1);

      Coords.Add(vv3X);
      Coords.Add(vv3Y);
      Coords.Add(vv3Z);
      Faces.Add(indexPointer + 2);

      Triangles.Add(
        new NavisworksDoubleTriangle(
          new NavisworksDoubleVertex(vv1X, vv1Y, vv1Z),
          new NavisworksDoubleVertex(vv2X, vv2Y, vv2Z),
          new NavisworksDoubleVertex(vv3X, vv3Y, vv3Z)
        )
      );
    }
  }

  public class NavisworksGeometry
  {
    public InwOpSelection ComSelection { get; set; }
    public ModelItem ModelItem { get; set; }
    public Stack<InwOaFragment3> ModelFragments { get; set; }
    public Base Geometry { get; internal set; }
    public Base Base { get; internal set; }

    public NavisworksGeometry(ModelItem modelItem)
    {
      ModelItem = modelItem;

      // Add conversion geometry to oModelColl Property
      ModelItemCollection modelItemCollection = new ModelItemCollection
      {
        modelItem
      };

      //convert to COM selection
      ComSelection = ComBridge.ToInwOpSelection(modelItemCollection);
    }

    public List<CallbackGeomListener> GetUniqueFragments()
    {
      List<CallbackGeomListener> callbackListeners = new List<CallbackGeomListener>();

      foreach (InwOaPath path in ComSelection.Paths())
      {
        CallbackGeomListener callbackListener = new CallbackGeomListener();
        foreach (InwOaFragment3 fragment in ModelFragments)
        {
          Array a1 = ((Array)fragment.path.ArrayData).ToArray<int>();
          Array a2 = ((Array)path.ArrayData).ToArray<int>();

          // This is now lots of duplicate code!!
          bool isSame = true;

          if (a1.Length == a2.Length)
          {
            for (int i = 0; i < a1.Length; i += 1)
            {
              int a1Value = (int)a1.GetValue(i);
              int a2Value = (int)a2.GetValue(i);

              if (a1Value == a2Value) continue;
              isSame = false;
              break;
            }
          }
          else
          {
            isSame = false;
          }

          if (!isSame) continue;

          InwLTransform3f3 localToWorld = (InwLTransform3f3)fragment.GetLocalToWorldMatrix();

          //create Global Coordinate System Matrix
          object matrix = localToWorld.Matrix;
          Array matrixArray = (Array)matrix;
          double[] elements = ConvertArrayToDouble(matrixArray);
          double[] elementsValue = new double[elements.Length];
          for (int i = 0; i < elements.Length; i++)
          {
            elementsValue[i] = elements[i];
          }

          callbackListener.Matrix = elementsValue;
          fragment.GenerateSimplePrimitives(nwEVertexProperty.eNORMAL, callbackListener);
        }

        callbackListeners.Add(callbackListener);
      }

      return callbackListeners;
    }

    public List<CallbackGeomListener> GetFragments()
    {
      List<CallbackGeomListener> callbackListeners = new List<CallbackGeomListener>();
      // create the callback object

      foreach (InwOaPath3 path in ComSelection.Paths())
      {
        CallbackGeomListener callbackListener = new CallbackGeomListener();
        foreach (InwOaFragment3 fragment in path.Fragments())
        {
          InwLTransform3f3 localToWorld = (InwLTransform3f3)fragment.GetLocalToWorldMatrix();

          //create Global Coordinate System Matrix
          object matrix = localToWorld.Matrix;
          Array matrixArray = (Array)matrix;
          double[] elements = ConvertArrayToDouble(matrixArray);
          double[] elementsValue = new double[elements.Length];
          for (int i = 0; i < elements.Length; i++)
          {
            elementsValue[i] = elements[i];
          }

          callbackListener.Matrix = elementsValue;
          fragment.GenerateSimplePrimitives(nwEVertexProperty.eNORMAL, callbackListener);
        }

        callbackListeners.Add(callbackListener);
      }

      return callbackListeners;
    }

    public T[] ToArray<T>(Array arr)
    {
      T[] result = new T[arr.Length];
      Array.Copy(arr, result, result.Length);
      return result;
    }

    public static double[] ConvertArrayToDouble(Array arr)
    {
      if (arr.Rank != 1)
      {
        throw new ArgumentException();
      }

      double[] doubleArray = new double[arr.GetLength(0)];
      for (int ix = arr.GetLowerBound(0); ix <= arr.GetUpperBound(0); ++ix)
      {
        doubleArray[ix - arr.GetLowerBound(0)] = (double)arr.GetValue(ix);
      }

      return doubleArray;
    }
  }

  public class NavisworksTriangle
  {
    public NavisworksVertex Vertex1 { get; set; }
    public NavisworksVertex Vertex2 { get; set; }
    public NavisworksVertex Vertex3 { get; set; }

    public NavisworksTriangle(NavisworksVertex v1, NavisworksVertex v2, NavisworksVertex v3)
    {
      Vertex1 = v1;
      Vertex2 = v2;
      Vertex3 = v3;
    }
  }

  public class NavisworksDoubleTriangle
  {
    public NavisworksDoubleVertex Vertex1 { get; set; }
    public NavisworksDoubleVertex Vertex2 { get; set; }
    public NavisworksDoubleVertex Vertex3 { get; set; }

    public NavisworksDoubleTriangle(NavisworksDoubleVertex v1, NavisworksDoubleVertex v2, NavisworksDoubleVertex v3)
    {
      Vertex1 = v1;
      Vertex2 = v2;
      Vertex3 = v3;
    }
  }

  public class NavisworksVertex
  {
    public NavisworksVertex(float x, float y, float z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
  }

  public class NavisworksDoubleVertex
  {
    public NavisworksDoubleVertex(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
  }

  public class NavisworksMesh
  {
    public List<int> Indices { get; set; }
    public List<float> Vertices { get; set; }
    public List<NavisworksTriangle> Triangles { get; set; }

    public NavisworksMesh(List<NavisworksTriangle> triangles)
    {
      this.Triangles = new List<NavisworksTriangle>();
      this.Triangles = triangles;

      //Add indices and vertices
      Indices = new List<int>();
      Vertices = new List<float>();
      int index = 0;

      //create indices and vertices lists
      foreach (NavisworksTriangle triangle in triangles)
      {
        Indices.Add(index++);
        Indices.Add(index++);
        Indices.Add(index++);
        Vertices.Add(triangle.Vertex1.X);
        Vertices.Add(triangle.Vertex1.Y);
        Vertices.Add(triangle.Vertex1.Z);
        Vertices.Add(triangle.Vertex2.X);
        Vertices.Add(triangle.Vertex2.Y);
        Vertices.Add(triangle.Vertex2.Z);
        Vertices.Add(triangle.Vertex3.X);
        Vertices.Add(triangle.Vertex3.Y);
        Vertices.Add(triangle.Vertex3.Z);
      }
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

    public Vector3D TransformVector3D { get; set; }
    public Vector SettingOutPoint { get; set; }
    public Vector TransformVector { get; set; }

    public Dictionary<int[], Stack<InwOaFragment3>> PathDictionary =
      new Dictionary<int[], Stack<InwOaFragment3>>();

    public Dictionary<NavisworksGeometry, Stack<InwOaFragment3>> ModelGeometryDictionary =
      new Dictionary<NavisworksGeometry, Stack<InwOaFragment3>>();

    public HashSet<NavisworksGeometry> GeometrySet = new HashSet<NavisworksGeometry>();

    public readonly ModelItemCollection SelectedItems = new ModelItemCollection();
    public readonly ModelItemCollection SelectedItemsAndDescendants = new ModelItemCollection();


    /// <summary>
    /// Parse all descendant nodes of the element that are visible, selected and geometry nodes.
    /// </summary>
    public List<ModelItem> CollectGeometryNodes(ModelItem element)
    {
      ModelItemEnumerableCollection descendants = element.DescendantsAndSelf;

      // if the descendant node isn't hidden, has geometry and is part of the original selection set.

      List<ModelItem> items = new List<ModelItem>();
      int dCount = descendants.Count();

      foreach (ModelItem item in descendants)
      {
        bool hasGeometry = item.HasGeometry;
        bool isVisible = !item.IsHidden;
        bool isSelected = SelectedItemsAndDescendants.IsSelected(item);

        if (hasGeometry && isVisible && isSelected)
        {
          items.Add(item);
        }

        Console.WriteLine($"Collecting Geometry Nodes {items.Count} of possible {dCount}", ConsoleColor.DarkYellow);
      }

      return items;
    }

    public void AddFragments(NavisworksGeometry geometry)
    {
      geometry.ModelFragments = new Stack<InwOaFragment3>();

      foreach (InwOaPath path in geometry.ComSelection.Paths())
      {
        foreach (InwOaFragment3 fragment in path.Fragments())
        {
          int[] a1 = ((Array)fragment.path.ArrayData).ToArray<int>();
          int[] a2 = ((Array)path.ArrayData).ToArray<int>();
          bool isSame = !(a1.Length != a2.Length || !a1.SequenceEqual(a2));

          if (isSame)
          {
            geometry.ModelFragments.Push(fragment);
          }
        }
      }
    }

    public void GetSortedFragments(ModelItemCollection modelItems)
    {
      InwOpSelection oSel = ComBridge.ToInwOpSelection(modelItems);
      // To be most efficient you need to lookup an efficient EqualityComparer
      // for the int[] key
      foreach (InwOaPath3 path in oSel.Paths())
      {
        // this yields ONLY unique fragments
        // ordered by geometry they belong to
        foreach (InwOaFragment3 fragment in path.Fragments())
        {
          int[] pathArr = ((Array)fragment.path.ArrayData).ToArray<int>();
          if (!PathDictionary.TryGetValue(pathArr, out Stack<InwOaFragment3> frags))
          {
            frags = new Stack<InwOaFragment3>();
            PathDictionary[pathArr] = frags;
          }

          frags.Push(fragment);
        }
      }
    }

    public void TranslateGeometryElement(NavisworksGeometry geometryElement)
    {
      Base elementBase = new Base();

      if (geometryElement.ModelItem.HasGeometry && !geometryElement.ModelItem.Children.Any())
      {
        List<Base> speckleGeometries = TranslateFragmentGeometry(geometryElement);
        if (speckleGeometries.Count > 0)
        {
          elementBase["displayValue"] = speckleGeometries;
          elementBase["units"] = "m";
          elementBase["bbox"] = BoxToSpeckle(geometryElement.ModelItem.BoundingBox());
        }
      }

      geometryElement.Geometry = elementBase;
    }

    public List<Base> TranslateFragmentGeometry(NavisworksGeometry navisworksGeometry)
    {
      List<CallbackGeomListener> callbackListeners = navisworksGeometry.GetUniqueFragments();

      List<Base> baseGeometries = new List<Base>();

      Vector3D move = (TransformVector3D == null) ? new Vector3D(0, 0, 0) : TransformVector3D;

      foreach (CallbackGeomListener callback in callbackListeners)
      {
        List<NavisworksDoubleTriangle> triangles = callback.Triangles;
        // TODO: Additional Geometry Types
        //List<NavisworksDoubleLine> Lines = callback.Lines;
        //List<NavisworksDoublePoint> Points = callback.Points;

        List<double> vertices = new List<double>();
        List<int> faces = new List<int>();


        // TODO: this needs to come from options. For now, no move.

        int triangleCount = triangles.Count;
        if (triangleCount <= 0) continue;
        for (int t = 0; t < triangleCount; t += 1)
        {
          Units source = Application.ActiveDocument.Units;
          Units target = Units.Meters;
          double scale = UnitConversion.ScaleFactor(source, target);

          // Apply the bounding box move.
          // The native API methods for overriding transforms are not thread safe to call from the CEF instance
          vertices.AddRange(new List<double>
          {
            (triangles[t].Vertex1.X + move.X) * scale,
            (triangles[t].Vertex1.Y + move.Y) * scale,
            (triangles[t].Vertex1.Z + move.Z) * scale
          });
          vertices.AddRange(new List<double>
          {
            (triangles[t].Vertex2.X + move.X) * scale,
            (triangles[t].Vertex2.Y + move.Y) * scale,
            (triangles[t].Vertex2.Z + move.Z) * scale
          });
          vertices.AddRange(new List<double>
          {
            (triangles[t].Vertex3.X + move.X) * scale,
            (triangles[t].Vertex3.Y + move.Y) * scale,
            (triangles[t].Vertex3.Z + move.Z) * scale
          });

          // TODO: Move this back to Geometry.cs
          faces.Add(0);
          faces.Add(t * 3);
          faces.Add(t * 3 + 1);
          faces.Add(t * 3 + 2);
        }

        Mesh baseMesh = new Mesh(vertices, faces)
        {
          ["renderMaterial"] = ConverterNavisworks.TranslateMaterial(navisworksGeometry.ModelItem)
        };
        baseGeometries.Add(baseMesh);
      }

      return baseGeometries; // TODO: Check if this actually has geometries before adding to DisplayValue
    }
  }
}