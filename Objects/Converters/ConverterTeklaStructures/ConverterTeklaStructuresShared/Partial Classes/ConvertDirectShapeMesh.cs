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
    public void MeshToNative(List<GE.Mesh> displayValues)
    {

      foreach (var mesh in displayValues)
      {

        var faces = mesh.faces;
        List<List<int>> faceList = new List<List<int>> { };
        faceList = faces.Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / 4)
        .Select(x => x.Select(v => v.Value).ToList())
        .ToList();
        var vertices = mesh.vertices;
        List<List<double>> verticesList = new List<List<double>> { };
        verticesList =  vertices.Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / 3)
        .Select(x => x.Select(v => v.Value).ToList())
        .ToList();
        Vector[] vertexs = new Vector[] { };
        int[][] outerWires = new int[][] { };
        var innerLoop = new Dictionary<int, int[][]> { };
        foreach(var vertex in verticesList){
          var teklaVectorVertex = new Vector(vertex[0], vertex[1], vertex[2]);
          vertexs.Append(teklaVectorVertex);
        }
        foreach(var face in faceList){
          var teklaFaceLoop = new[] { face[1], face[2], face[3] };
          outerWires.Append(teklaFaceLoop);
        }
        var brep = new FacetedBrep(vertexs, outerWires, innerLoop);

        var shapeItem = new ShapeItem
        {
          Name = "Test",
          ShapeFacetedBrep = brep,
          UpAxis = ShapeUpAxis.Z_Axis
        };
        shapeItem.Insert();
        //Model.CommitChanges();
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
  }

}

