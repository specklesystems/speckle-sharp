#if (CIVIL2021 || CIVIL2022)
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilDB = Autodesk.Civil.DatabaseServices;
using Acad = Autodesk.AutoCAD.Geometry;

using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Curve = Objects.Geometry.Curve;
using Brep = Objects.Geometry.Brep;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // featurelines
    public Polycurve FeatureLineToSpeckle(CivilDB.FeatureLine featureLine)
    {
      var polycurve = new Polycurve() { closed = featureLine.Closed };

      // extract segment curves
      var segments = new List<ICurve>();
      var exploded = new DBObjectCollection();
      featureLine.Explode(exploded);
      for (int i = 0; i < exploded.Count; i++)
        segments.Add((ICurve)ConvertToSpeckle(exploded[i]));
      polycurve.segments = segments;

      // TODO: additional params to attach
      var points = featureLine.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
      var grade = new Interval(featureLine.MinGrade, featureLine.MaxGrade);
      var elevation = new Interval(featureLine.MinElevation, featureLine.MaxElevation);
      var name = featureLine.DisplayName;

      return polycurve;
    }
    public CivilDB.FeatureLine FeatureLineToNative(Polycurve polycurve)
    {
      return null;
    }

    // alignments
    public Curve AlignmentToSpeckle(CivilDB.Alignment alignment)
    {
      var curve = CurveToSpeckle(alignment.BaseCurve, ModelUnits) as Curve;
      curve["name"] = alignment.DisplayName;
      curve["description"] = alignment.Description;
      try{
      curve["startStation"] = alignment.StartingStation;
      curve["endStation"] = alignment.EndingStation;
      }
      catch{}
      return curve;
    }

    // profiles
    public Curve ProfileToSpeckle(CivilDB.Profile profile)
    {
      var curve = CurveToSpeckle(profile.BaseCurve, ModelUnits) as Curve;
      curve["name"] = profile.DisplayName;
      curve["description"] = profile.Description;
      curve["profileType"] = profile.ProfileType.ToString();
      try{
      curve["startStation"] = profile.StartingStation;
      curve["endStation"] = profile.EndingStation;
      curve["offset"] = profile.Offset;
      }
      catch{}

      return curve;
    }

    // surfaces
    public Mesh SurfaceToSpeckle(CivilDB.TinSurface surface)
    {
      Mesh mesh = null;

      // output vars
      var _vertices = new List<Acad.Point3d>();
      var _faces = new List<int[]>();

      foreach (var triangle in surface.GetTriangles(false))
      {
        // get vertices
        var faceIndices = new List<int>();
        foreach (var vertex in new List<CivilDB.TinSurfaceVertex>() {triangle.Vertex1, triangle.Vertex2, triangle.Vertex3})
        {
          if (!_vertices.Contains(vertex.Location))
          {
            faceIndices.Add(_vertices.Count);
            _vertices.Add(vertex.Location);
          }
          else
          {
            faceIndices.Add(_vertices.IndexOf(vertex.Location));
          }
          vertex.Dispose();
        }

        // get face
        _faces.Add(new int[] { 0, faceIndices[0], faceIndices[1], faceIndices[2] });

        triangle.Dispose();
      }

      var vertices = PointsToFlatArray(_vertices);
      var faces = _faces.SelectMany(o => o).ToArray();
      mesh = new Mesh(vertices, faces);
      mesh.units = ModelUnits;
      mesh.bbox = BoxToSpeckle(surface.GeometricExtents);

      // add tin surface props
      mesh["name"] = surface.DisplayName;
      mesh["description"] = surface.Description;

      return mesh;
    }

    public Mesh SurfaceToSpeckle(CivilDB.GridSurface surface)
    {
      Mesh mesh = null;

      // output vars
      var _vertices = new List<Acad.Point3d>();
      var _faces = new List<int[]>();

      foreach (var cell in surface.GetCells(false))
      {
        // get vertices
        var faceIndices = new List<int>();
        foreach (var vertex in new List<CivilDB.GridSurfaceVertex>() {cell.BottomLeftVertex, cell.BottomRightVertex, cell.TopLeftVertex, cell.TopRightVertex})
        {
          if (!_vertices.Contains(vertex.Location))
          {
            faceIndices.Add(_vertices.Count);
            _vertices.Add(vertex.Location);
          }
          else
          {
            faceIndices.Add(_vertices.IndexOf(vertex.Location));
          }
          vertex.Dispose();
        }

        // get face
        _faces.Add(new int[] { 1, faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3] });

        cell.Dispose();
      }

      var vertices = PointsToFlatArray(_vertices);
      var faces = _faces.SelectMany(o => o).ToArray();
      mesh = new Mesh(vertices, faces);
      mesh.units = ModelUnits;
      mesh.bbox = BoxToSpeckle(surface.GeometricExtents);

      // add grid surface props
      mesh["name"] = surface.DisplayName;
      mesh["description"] = surface.Description;

      return mesh;
    }

    // structures
    public Mesh StructureToSpeckle(CivilDB.Structure structure)
    {
      var mesh = SolidToSpeckle(structure.Solid3dBody);

      // assign additional structure props
      mesh["@baseCurve"] = CurveToSpeckle(structure.BaseCurve, ModelUnits) as Curve;
      mesh["name"] = structure.DisplayName;
      mesh["description"] = structure.Description;
      mesh["connectedPipes"] = structure.ConnectedPipesCount;
      mesh["@location"] = PointToSpeckle(structure.Location, ModelUnits);
      try{
      mesh["station"] = structure.Station;
      }
      catch{}
      mesh["network"] = structure.NetworkName;

      return mesh;
    }

    // pipes
    public Mesh PipeToSpeckle(CivilDB.Pipe pipe)
    {
      var mesh = SolidToSpeckle(pipe.Solid3dBody);

      // assign additional structure props
      mesh["@baseCurve"] = CurveToSpeckle(pipe.BaseCurve, ModelUnits) as Curve;
      mesh["name"] = pipe.DisplayName;
      mesh["description"] = pipe.Description;
      mesh["flowDirection"] = pipe.FlowDirection.ToString();
      mesh["flowRate"] = pipe.FlowRate;
      mesh["network"] = pipe.NetworkName;
      try{
      mesh["startStation"] = pipe.StartStation;
      mesh["endStation"] = pipe.EndStation;
      mesh["startOffset"] = pipe.StartOffset;
      mesh["endOffset"] = pipe.EndOffset;
      }
      catch{}
      return mesh;
    }
  }
}
#endif