using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Objects.Geometry
{
  public class Brep : Base, IGeometry
  {
    public object rawData { get; set; }
    public string provenance { get; set; }
    public Mesh displayValue { get; set; }

    public List<Surface> Surfaces { get; set; }
    public List<Curve> Curve3D { get; set; }
    public List<Curve> Curve2D { get; set; }
    public List<BrepVertex> Vertices { get; set; }
    public List<BrepEdge> Edges { get; set; }
    public List<BrepLoop> Loops { get; set; }
    public List<BrepTrim> Trims { get; set; }
    public List<BrepFace> Faces { get; set; }

    public Brep()
    {
      Surfaces = new List<Surface>();
      Curve2D = new List<Curve>();
      Curve3D = new List<Curve>();

      Vertices = new List<BrepVertex>();
      Edges = new List<BrepEdge>();
      Loops = new List<BrepLoop>();
      Trims = new List<BrepTrim>();
      Faces = new List<BrepFace>();
    }

    public Brep(object rawData, string provenance, Mesh displayValue, string applicationId = null) : this()
    {
      this.rawData = rawData;
      this.provenance = provenance;
      this.displayValue = displayValue;
      this.applicationId = applicationId;
    }

    [OnDeserialized]
    internal void onDeserialized(StreamingContext context)
    {
      Edges.ForEach(e => e.Brep = this);
      Loops.ForEach(l => l.Brep = this);
      Trims.ForEach(t => t.Brep = this);
      Faces.ForEach(f => f.Brep = this);
    }
  }

  /// <summary>
  /// Represents a face on a <see cref="Brep"/>
  /// </summary>
  public class BrepFace : Base
  {
    [JsonIgnore]
    public Brep Brep { get; set; }
    public int SurfaceIndex { get; set; }
    public List<int> LoopIndices { get; set; }
    public int OuterLoopIndex { get; set; }
    public bool OrientationReversed { get; set; }

    public BrepFace()
    {
    }

    public BrepFace(Brep brep, int surfaceIndex, List<int> loopIndices, int outerLoopIndex, bool orientationReversed)
    {
      Brep = brep;
      SurfaceIndex = surfaceIndex;
      LoopIndices = loopIndices;
      OuterLoopIndex = outerLoopIndex;
      OrientationReversed = orientationReversed;
    }

    [JsonIgnore] public BrepLoop OuterLoop => Brep.Loops[OuterLoopIndex];
    [JsonIgnore] public Surface Surface => Brep.Surfaces[SurfaceIndex];
    [JsonIgnore] public List<BrepLoop> Loops => LoopIndices.Select(i => Brep.Loops[i]).ToList();
  }

  /// <summary>
  /// Represents a UV Trim Closed Loop on one of the <see cref="Brep"/>'s surfaces.
  /// </summary>
  public class BrepLoop : Base
  {
    [JsonIgnore]
    public Brep Brep { get; set; }
    public int FaceIndex { get; set; }
    public List<int> TrimIndices { get; set; }
    public BrepLoopType Type { get; set; }

    public BrepLoop()
    {
    }

    public BrepLoop(Brep brep, int faceIndex, List<int> trimIndices, BrepLoopType type)
    {
      Brep = brep;
      FaceIndex = faceIndex;
      TrimIndices = trimIndices;
      Type = type;
    }

    [JsonIgnore] public BrepFace Face => Brep.Faces[FaceIndex];
    [JsonIgnore] public List<BrepTrim> Trims => TrimIndices.Select(i => Brep.Trims[i]).ToList();
  }

  public class BrepTrim : Base
  {
    [JsonIgnore]
    public Brep Brep { get; set; }
    public int EdgeIndex { get; set; }
    public int FaceIndex { get; set; }
    public int LoopIndex { get; set; }
    public int CurveIndex { get; set; }
    public int IsoStatus { get; set; }
    public int TrimType { get; set; }
    public bool IsReversed { get; set; }

    public BrepTrim()
    {
    }

    public BrepTrim(Brep brep, int edgeIndex, int faceIndex, int loopIndex, int curveIndex, int isoStatus, int trimType,
      bool reversed)
    {
      Brep = brep;
      EdgeIndex = edgeIndex;
      FaceIndex = faceIndex;
      LoopIndex = loopIndex;
      CurveIndex = curveIndex;
      IsoStatus = isoStatus;
      TrimType = trimType;
      IsReversed = reversed;
    }

    [JsonIgnore] public BrepFace Face => Brep.Faces[FaceIndex];

    [JsonIgnore] public BrepLoop Loop => Brep.Loops[LoopIndex];
  }

  public class BrepEdge : Base
  {
    [JsonIgnore]
    public Brep Brep { get; set; }
    public int Curve3dIndex { get; set; }
    public int[] TrimIndices { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }

    public BrepEdge()
    {
    }

    public BrepEdge(Brep brep, int curve3dIndex, int[] trimIndices, int startIndex, int endIndex)
    {
      Brep = brep;
      Curve3dIndex = curve3dIndex;
      TrimIndices = trimIndices;
      StartIndex = startIndex;
      EndIndex = endIndex;
    }

    [JsonIgnore] public BrepVertex StartVertex => Brep.Vertices[StartIndex];
    [JsonIgnore] public BrepVertex EndVertex => Brep.Vertices[EndIndex];
    [JsonIgnore] public IEnumerable<BrepTrim> Trims => TrimIndices.Select(i => Brep.Trims[i]);
    [JsonIgnore] public ICurve Curve => Brep.Curve3D[Curve3dIndex];
  }

  public class BrepVertex : Base
  {
    public BrepVertex()
    {
    }

    public BrepVertex(Point location)
    {
      Location = location;
    }

    public Point Location { get; set; }
  }

  public enum BrepLoopType
  {
    Unknown = 0,
    Inner = 2,
    Outer = 1,
  }
}