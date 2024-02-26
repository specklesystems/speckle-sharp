using System;
using System.Collections.Generic;
using GE = Objects.Geometry;
using Speckle.Core.Models;
using System.Linq;
using Speckle.Core.Kits;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Catalogs;

namespace Objects.Converter.TeklaStructures;

public partial class ConverterTeklaStructures
{
  /// <summary>
  /// Converts a list of Mesh objects to their native representations.
  /// </summary>
  /// <param name="object">The base object being converted.</param>
  /// <param name="displayValues">A list of GE.Mesh objects to be converted.</param>
  /// <remarks>
  /// Exception handling is specifically tailored to the process: An ArgumentException indicating
  /// that "The BRep geometry already exists" is intentionally swallowed, reflecting a scenario
  /// where a duplicate shape is encountered and can be safely ignored. All other exceptions,
  /// previously swallowed, are now propagated as ConversionExceptions, signaling a failure in
  /// conversion or insertion.
  /// </remarks>
  /// <exception cref="ConversionException">Thrown for any errors during conversion or insertion,
  /// except when encountering duplicate BRep geometry.</exception>
  public void MeshToNative(Base @object, List<GE.Mesh> displayValues)
  {
    int shapeCounter = 0;
    foreach (var mesh in displayValues)
    {
      FacetedBrep facetedBrep = CreateFacetedBrep(mesh);

      // Add shape to catalog
      // Each shape in catalog needs to have a unique name
      string objectName = GetShapeName(@object);
      string shapeName = "Speckle_" + objectName + (shapeCounter > 0 ? "_" + shapeCounter.ToString() : "");
      var shapeItem = new ShapeItem
      {
        Name = shapeName,
        ShapeFacetedBrep = facetedBrep,
        UpAxis = ShapeUpAxis.Z_Axis
      };
      // remove possibly pre-existing shape with same name and then insert a Speckle object shape
      // with that name into the catalog
      shapeItem.Delete();

      bool result = false;
      try
      {
        result = shapeItem.Insert();
      }
      catch (InvalidOperationException ex)
      {
        throw new ConversionException("The converted Mesh could not be inserted.", ex);
      }
      catch (ArgumentException ex)
      {
        // if the exception message contains "BRep already exists", swallow the error, else rethrow
        if (!ex.Message.Contains("The BRep geometry already exists"))
        {
          throw new ConversionException($"Failed to convert Mesh to Native: {ex.Message} ", ex);
        }
      }

      if (!result)
      {
        // Find pre-existing shape in catalog with same fingerprint
        var matchingShape = CheckFingerprint(shapeItem);
        if (matchingShape != null)
        {
          shapeName = matchingShape.Name;
          result = true;
        }
        else
        {
          throw new ConversionException(
            "The Mesh could either not be converted or not be inserted but no internal error occurred."
          );
        }
      }

      // Insert object in model
      if (result)
      {
        var brep = new Brep
        {
          StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0),
          EndPoint = new Tekla.Structures.Geometry3d.Point(1000, 0, 0),
          Profile = { ProfileString = shapeName },
          Material = { MaterialString = "TS_Undefined" },
          Position =
          {
            Depth = Position.DepthEnum.MIDDLE,
            Plane = Position.PlaneEnum.MIDDLE,
            Rotation = Position.RotationEnum.TOP
          }
        };
        brep.Insert();
      }

      shapeCounter++;
    }

    //var vertex = new[]
    //  {
    //      new Vector(0.0, 0.0, 0.0), // 0
    //      new Vector(300.0, 0.0, 0.0), // 1
    //      new Vector(300.0, 700.0, 0.0), // 2
    //      new Vector(0.0, 700.0, 0.0), // 3
    //      new Vector(300.0, 700.0, 0.0), // 4
    //      new Vector(300.0, 700.0, 2000.0), // 5
    //      new Vector(0.0, 700.0, 2000.0), // 6
    //      new Vector(100.0, 100.0, 0.0), // 7
    //      new Vector(200.0, 100.0, 0.0), // 8
    //      new Vector(200.0, 200.0, 0.0), // 9
    //      new Vector(100.0, 200.0, 0.0) // 10
    //  };
    //var outerWires = new[]
    //{
    //foreach
    //  };
    //var innerWires = new Dictionary<int, int[][]>
    //  {

    //  };

    //var brep = new FacetedBrep(vertex, outerWires, innerWires);

    //var shapeItem = new ShapeItem
    //{
    //  Name = "Test",
    //  ShapeFacetedBrep = brep,
    //  UpAxis = ShapeUpAxis.Z_Axis
    //};
    //shapeItem.Insert();
    //Model.CommitChanges();
  }

  public FacetedBrep CreateFacetedBrep(GE.Mesh mesh)
  {
    var faces = mesh.faces;
    List<List<int>> faceList = new() { };
    faceList = faces
      .Select((x, i) => new { Index = i, Value = x })
      .GroupBy(x => x.Index / 4)
      .Select(x => x.Select(v => v.Value).ToList())
      .ToList();
    var vertices = mesh.vertices;
    List<List<double>> verticesList = new() { };
    verticesList = vertices
      .Select((x, i) => new { Index = i, Value = x })
      .GroupBy(x => x.Index / 3)
      .Select(x => x.Select(v => v.Value).ToList())
      .ToList();
    List<Vector> vertexs = new();
    List<int[]> outerWires = new();
    var innerLoop = new Dictionary<int, int[][]> { };
    foreach (var vertex in verticesList)
    {
      var teklaVectorVertex = new Vector(vertex[0], vertex[1], vertex[2]);
      vertexs.Add(teklaVectorVertex);
    }
    foreach (var face in faceList)
    {
      // Tekla wants the face loops in reverse
      var teklaFaceLoop = new[] { face[3], face[2], face[1] };
      outerWires.Add(teklaFaceLoop);
    }
    var brep = new FacetedBrep(vertexs.ToArray(), outerWires.ToArray(), innerLoop);
    return brep;
  }

  public string GetShapeName(Base @object)
  {
    string name = "";

    // Take application id
    if (string.IsNullOrEmpty(name))
    {
      name = @object.applicationId;
    }

    // If still empty then do Speckle id but can cause failure since changes with every commit
    if (string.IsNullOrEmpty(name))
    {
      name = @object.id;
    }

    return name;
  }

  public ShapeItem CheckFingerprint(ShapeItem si)
  {
    CatalogHandler catalogHandler = new();
    ShapeItemEnumerator sie = catalogHandler.GetShapeItems();
    while (sie.MoveNext())
    {
      ShapeItem siItem = sie.Current;
      if (siItem.Fingerprint == Polymesh.Fingerprint(si.ShapeFacetedBrep))
      {
        return siItem;
      }
    }
    return null;
  }
}
