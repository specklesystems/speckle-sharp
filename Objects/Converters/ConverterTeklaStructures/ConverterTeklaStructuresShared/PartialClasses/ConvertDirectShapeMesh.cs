using System;
using System.Collections.Generic;
using GE = Objects.Geometry;
using GES = Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.BuiltElements.TeklaStructures;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Catalogs;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public void MeshToNative(Base @object, List<GE.Mesh> displayValues)
    {
      int incr = 0;
      foreach (var mesh in displayValues)
      {
        FacetedBrep facetedBrep = CreateFacetedBrep(mesh);

        // Add shape to catalog
        // Each shape in catalog needs to have a unique name
        string objectName = GetShapeName(@object);
        string shapeName = "Speckle_" + objectName + (incr > 0 ? "_" + incr.ToString() : "");
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
          // Fails if two shapes exist with different names but same geometry fingerprint
        }
        catch (Exception ex) { }

        if (!result)
        {
          // Find pre-existing shape in catalog with same fingerprint
          var matchingShape = CheckFingerprint(shapeItem);
          if (matchingShape != null)
          {
            shapeName = matchingShape.Name;
            result = true;
          }
        }

        // Insert object in model
        if (result)
        {
          var brep = new Brep();
          brep.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
          brep.EndPoint = new Tekla.Structures.Geometry3d.Point(1000, 0, 0);
          brep.Profile.ProfileString = shapeName;
          brep.Material.MaterialString = "TS_Undefined";
          brep.Position.Depth = Position.DepthEnum.MIDDLE;
          brep.Position.Plane = Position.PlaneEnum.MIDDLE;
          brep.Position.Rotation = Position.RotationEnum.TOP;
          brep.Insert(); 
        }

        incr++;
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
        List<List<int>> faceList = new List<List<int>> { };
        faceList = faces.Select((x, i) => new { Index = i, Value = x })
          .GroupBy(x => x.Index / 4)
          .Select(x => x.Select(v => v.Value).ToList())
          .ToList();
        var vertices = mesh.vertices;
        List<List<double>> verticesList = new List<List<double>> { };
        verticesList = vertices.Select((x, i) => new { Index = i, Value = x })
          .GroupBy(x => x.Index / 3)
          .Select(x => x.Select(v => v.Value).ToList())
          .ToList();
        List<Vector> vertexs = new List<Vector>();
        List<int[]> outerWires = new List<int[]>();
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
        name = @object.applicationId;

      // If still empty then do Speckle id but can cause failure since changes with every commit
      if (string.IsNullOrEmpty(name))
        name = @object.id;

      return name;
    }
    public ShapeItem CheckFingerprint(ShapeItem si)
    {
      CatalogHandler catalogHandler = new CatalogHandler();
      ShapeItemEnumerator sie = catalogHandler.GetShapeItems();
      while(sie.MoveNext())
      {
        ShapeItem siItem = sie.Current;
        if (siItem.Fingerprint == Polymesh.Fingerprint(si.ShapeFacetedBrep))
          return siItem;
      }
      return null;
    }
  }

}

