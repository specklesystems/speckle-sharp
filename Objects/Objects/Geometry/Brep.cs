using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Objects.Primitive;
using Speckle.Core.Kits;
using static Speckle.Core.Models.Utilities;


namespace Objects.Geometry
{
  public class Brep : Base, IHasArea, IHasVolume, IHasBoundingBox
  {
    public string provenance { get; set; }
    public Box bbox { get; set; }
    public double area { get; set; }
    public double volume { get; set; }

    [DetachProperty]
    public Mesh displayValue { get; set; }

    /// <summary>
    /// Gets or sets the list of surfaces in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(200)]
    public List<Surface> Surfaces { get; set; }

    /// <summary>
    /// Gets or sets the list of 3-dimensional curves in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(200)]
    public List<ICurve> Curve3D { get; set; }

    /// <summary>
    /// Gets or sets the list of 2-dimensional UV curves in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(200)]
    public List<ICurve> Curve2D { get; set; }

    /// <summary>
    /// Gets or sets the list of vertices in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(5000)]
    public List<Point> Vertices { get; set; }

    /// <summary>
    /// Gets or sets the list of edges in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(5000)]
    public List<BrepEdge> Edges { get; set; }

    /// <summary>
    /// Gets or sets the list of closed UV loops in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(5000)]
    public List<BrepLoop> Loops { get; set; }

    /// <summary>
    /// Gets or sets the list of UV trim segments for each surface in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(5000)]
    public List<BrepTrim> Trims { get; set; }

    /// <summary>
    /// Gets or sets the list of faces in this <see cref="Brep"/> instance.
    /// </summary>
    [DetachProperty]
    [Chunkable(5000)]
    public List<BrepFace> Faces { get; set; }

    /// <summary>
    /// Gets or sets if this <see cref="Brep"/> instance is closed or not.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Gets or sets the list of surfaces in this <see cref="Brep"/> instance.
    /// </summary>
    public BrepOrientation Orientation { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="Brep"/> class.
    /// </summary>
    public Brep()
    {
      Surfaces = new List<Surface>();
      Curve2D = new List<ICurve>();
      Curve3D = new List<ICurve>();

      Vertices = new List<Point>();
      Edges = new List<BrepEdge>();
      Loops = new List<BrepLoop>();
      Trims = new List<BrepTrim>();
      Faces = new List<BrepFace>();

      IsClosed = false;
      Orientation = BrepOrientation.None;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Brep"/> class.
    /// </summary>
    /// <param name="provenance"></param>
    /// <param name="displayValue"></param>
    /// <param name="applicationId"></param>
    public Brep(string provenance, Mesh displayValue, string units = Units.Meters, string applicationId = null) : this()
    {
      this.provenance = provenance;
      this.displayValue = displayValue;
      this.applicationId = applicationId;
      this.units = units;
    }


    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
      Edges.ForEach(e => e.Brep = this);
      Loops.ForEach(l => l.Brep = this);
      Trims.ForEach(t => t.Brep = this);
      Faces.ForEach(f => f.Brep = this);

      //TODO: all the data props to the real props
    }
  }

  /// <summary>
  /// Represents the orientation of a <see cref="Brep"/>
  /// </summary>
  public enum BrepOrientation
  {
    None = 0,
    Inward = -1,
    Outward = 1,
    Unkown = 2
  }

  /// <summary>
  /// Represents a face on a <see cref="Brep"/>
  /// </summary>
  public class BrepFace : Base
  {
    [JsonIgnore] public Brep Brep { get; set; }
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
    [JsonIgnore] public Brep Brep { get; set; }
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

  /// <summary>
  /// Represents a UV Trim curve for one of the <see cref="Brep"/>'s surfaces.
  /// </summary>
  public class BrepTrim : Base
  {
    [JsonIgnore] public Brep Brep { get; set; }
    public int EdgeIndex { get; set; }
    
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int FaceIndex { get; set; }
    public int LoopIndex { get; set; }
    public int CurveIndex { get; set; }
    public int IsoStatus { get; set; }
    public BrepTrimType TrimType { get; set; }
    public bool IsReversed { get; set; }
    
    public Interval Domain { get; set; }
    
    public BrepTrim()
    {
    }

    public BrepTrim(Brep brep, int edgeIndex, int faceIndex, int loopIndex, int curveIndex, int isoStatus,
      BrepTrimType trimType, bool reversed, int startIndex, int endIndex)
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

    [JsonIgnore] public BrepEdge Edge => EdgeIndex != -1 ? Brep.Edges[EdgeIndex] : null;

    [JsonIgnore] public ICurve Curve2d => Brep.Curve2D[CurveIndex];
  }

  /// <summary>
  /// Represents an edge of the <see cref="Brep"/>.
  /// </summary>
  public class BrepEdge : Base
  {
    [JsonIgnore] public Brep Brep { get; set; }
    public int Curve3dIndex { get; set; }
    public int[] TrimIndices { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; } 
    
    public bool ProxyCurveIsReversed { get; set; }
    
    public Interval Domain { get; set; }
    public BrepEdge()
    {
    }

    public BrepEdge(Brep brep, int curve3dIndex, int[] trimIndices, int startIndex, int endIndex,
      bool proxyCurvedIsReversed, Interval domain)
    {
      Brep = brep;
      Curve3dIndex = curve3dIndex;
      TrimIndices = trimIndices;
      StartIndex = startIndex;
      EndIndex = endIndex;
      ProxyCurveIsReversed = proxyCurvedIsReversed;
      Domain = domain;
    }

    [JsonIgnore] public Point StartVertex => Brep.Vertices[StartIndex];
    [JsonIgnore] public Point EndVertex => Brep.Vertices[EndIndex];
    [JsonIgnore] public IEnumerable<BrepTrim> Trims => TrimIndices.Select(i => Brep.Trims[i]);
    [JsonIgnore] public ICurve Curve => Brep.Curve3D[Curve3dIndex];
  }


  /// <summary>
  /// Represents the type of a loop in a <see cref="Brep"/>'s face.
  /// </summary>
  public enum BrepLoopType
  {
    Unknown,
    Outer,
    Inner,
    Slit,
    CurveOnSurface,
    PointOnSurface,
  }

  public enum BrepTrimType
  {
    Unknown,
    Boundary,
    Mated,
    Seam,
    Singular,
    CurveOnSurface,
    PointOnSurface,
    Slit
  }
}