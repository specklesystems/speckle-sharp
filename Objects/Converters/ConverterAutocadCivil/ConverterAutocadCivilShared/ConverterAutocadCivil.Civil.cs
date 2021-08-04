#if (CIVIL2021 || CIVIL2022)
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilDB = Autodesk.Civil.DatabaseServices;
using Acad = Autodesk.AutoCAD.Geometry;

using Alignment = Objects.BuiltElements.Alignment;
using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Curve = Objects.Geometry.Curve;
using Point = Objects.Geometry.Point;
using Brep = Objects.Geometry.Brep;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Polyline = Objects.Geometry.Polyline;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // stations
    public Station StationToSpeckle(CivilDB.Station station)
    {
      var _station = new Station();
      _station.location = PointToSpeckle(station.Location);
      _station.type = station.StationType.ToString();
      _station.number = station.RawStation;
      _station.units = ModelUnits;

      return _station;
    }

    public CivilDB.FeatureLine FeatureLineToNative(Polycurve polycurve)
    {
      return null;
    }

    // alignments
    public Alignment AlignmentToSpeckle(CivilDB.Alignment alignment)
    {
      var _alignment = new Alignment();

      _alignment.baseCurve = CurveToSpeckle(alignment.BaseCurve, ModelUnits);
      if (alignment.DisplayName != null)
        _alignment.name = alignment.DisplayName;
      if (alignment.StartingStation != null)
        _alignment.startStation  = alignment.StartingStation;
      if (alignment.EndingStation != null)
        _alignment.endStation = alignment.EndingStation;

      // handle station equations
      var equations = new List<double>();
      var directions = new List<bool>();
      foreach (var stationEquation in alignment.StationEquations)
      {
        equations.AddRange(new List<double> { stationEquation.RawStationBack, stationEquation.StationBack, stationEquation.StationAhead });
        bool equationIncreasing = (stationEquation.EquationType.Equals(CivilDB.StationEquationType.Increasing)) ? true : false;
        directions.Add(equationIncreasing);
      }
      _alignment.stationEquations = equations;
      _alignment.stationEquationDirections = directions;

      _alignment.units = ModelUnits;

      return _alignment;
    }

    // profiles
    public Base ProfileToSpeckle(CivilDB.Profile profile)
    {
      var curve = CurveToSpeckle(profile.BaseCurve, ModelUnits) as Base;

      if (profile.DisplayName != null)
        curve["name"] = profile.DisplayName;
      if (profile.Description != null)
        curve["description"] = profile.Description;
      if (profile.StartingStation != null)
        curve["startStation"] = profile.StartingStation;
      if (profile.EndingStation != null)
        curve["endStation"] = profile.EndingStation;
      curve["profileType"] = profile.ProfileType.ToString();
      curve["offset"] = profile.Offset;
      curve.units = ModelUnits;

      return curve;
    }

    // featurelines
    public Base FeatureLineToSpeckle(CivilDB.FeatureLine featureline)
    {
      var curve = CurveToSpeckle(featureline.BaseCurve, ModelUnits) as Base;

      if (featureline.DisplayName != null)
        curve["name"] = featureline.DisplayName;
      if (featureline.Description != null)
        curve["description"] = featureline.Description;
      curve.units = ModelUnits;
      return curve;
    }
    /*
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
      try
      {
        var grade = new Interval(featureLine.MinGrade, featureLine.MaxGrade);
        var elevation = new Interval(featureLine.MinElevation, featureLine.MaxElevation);
        var name = featureLine.DisplayName;
      }
      catch { }

      return polycurve;
    }
    */

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
      try{
      mesh["name"] = surface.DisplayName;
      mesh["description"] = surface.Description;
      }
      catch{}

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
      try{
      mesh["name"] = surface.DisplayName;
      mesh["description"] = surface.Description;
      }
      catch{}

      return mesh;
    }

    // structures
    public Structure StructureToSpeckle(CivilDB.Structure structure)
    {
      // get ids pipes that are connected to this structure
      var pipeIds = new List<string>();
      for (int i = 0; i < structure.ConnectedPipesCount; i++)
        pipeIds.Add(structure.get_ConnectedPipe(i).ToString());

      var _structure = new Structure();

      _structure.location = PointToSpeckle(structure.Location, ModelUnits);
      _structure.pipeIds = pipeIds;
      _structure.displayMesh = SolidToSpeckle(structure.Solid3dBody);
      _structure.units = ModelUnits;

      // assign additional structure props
      _structure["name"] = (structure.DisplayName != null) ? structure.DisplayName : "";
      _structure["description"] = (structure.Description != null) ? structure.Description : "";
      try{ _structure["grate"] = structure.Grate; } catch{ }
      try{ _structure["station"] = structure.Station; } catch{ }
      try{ _structure["network"] = structure.NetworkName; } catch{ }

      return _structure;
    }

    // pipes
    public Pipe PipeToSpeckle(CivilDB.Pipe pipe)
    {
      // get the pipe curve
      ICurve curve = null;
      switch (pipe.SubEntityType)
      {
        case CivilDB.PipeSubEntityType.Straight:
          var line = new Line(pipe.StartPoint, pipe.EndPoint);
          curve = CurveToSpeckle(line);
          break;
        default:
          curve = CurveToSpeckle(pipe.Spline);
          break;
      }

      var _pipe = new Pipe();
      _pipe.baseCurve = curve;
      _pipe.diameter = pipe.InnerDiameterOrWidth;
      _pipe.length = pipe.Length3DToInsideEdge;
      _pipe.displayMesh = SolidToSpeckle(pipe.Solid3dBody);
      _pipe.units = ModelUnits;

      // assign additional structure props
      _pipe["name"] = (pipe.DisplayName != null) ? pipe.DisplayName : "";
      _pipe["description"] = (pipe.DisplayName != null) ? pipe.Description : "";
      try{ _pipe["shape"] = pipe.CrossSectionalShape.ToString(); } catch{ }
      try{ _pipe["slope"] = pipe.Slope; } catch{ }
      try{ _pipe["flowDirection"] = pipe.FlowDirection.ToString(); } catch{ }
      try{ _pipe["flowRate"] = pipe.FlowRate; } catch{ }
      try{ _pipe["network"] = pipe.NetworkName; } catch{ }
      try{ _pipe["startOffset"] = pipe.StartOffset; } catch{ }
      try{ _pipe["endOffset"] = pipe.EndOffset; } catch{ }
      try{ _pipe["startStation"] = pipe.StartStation; } catch{ }
      try{ _pipe["endStation"] = pipe.EndStation; } catch{ }

      return _pipe;
    }

    // corridors
    public Base CorridorToSpeckle(CivilDB.Corridor corridor)
    {
      var _corridor = new Base();

      var baselines = new List<Base>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (var baseline in corridor.Baselines)
        {
          Base convertedBaseline = null;
          if (baseline.IsFeatureLineBased())
          {
            var featureline = tr.GetObject(baseline.FeatureLineId, OpenMode.ForRead) as CivilDB.FeatureLine;
            convertedBaseline = FeatureLineToSpeckle(featureline);
          }
          else
          {
            var alignment = tr.GetObject(baseline.AlignmentId, OpenMode.ForRead) as CivilDB.Alignment;
            convertedBaseline = AlignmentToSpeckle(alignment);
          }
          if (convertedBaseline != null)
          {
            convertedBaseline["stations"] = baseline.SortedStations();
            baselines.Add(convertedBaseline);
          }
        }

        tr.Commit();
      }

      _corridor["@baselines"] = baselines;
      if (corridor.DisplayName != null)
        _corridor["name"] = corridor.DisplayName;
      if (corridor.Description != null)
        _corridor["description"] = corridor.Description;
      _corridor.units = ModelUnits;

      return _corridor;
    }

  }
}
#endif