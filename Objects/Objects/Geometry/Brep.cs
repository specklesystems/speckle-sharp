using System;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Objects.Other;
using Objects.Primitive;


namespace Objects.Geometry
{
  public class Brep : Base, IHasArea, IHasVolume, IHasBoundingBox, ITransformable<Brep>, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public string provenance { get; set; }
    public Box bbox { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public string units { get; set; }
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    /// <summary>
    /// Gets or sets the list of surfaces in this <see cref="Brep"/> instance.
    /// </summary>
    [JsonIgnore]
    public List<Surface> Surfaces { get; set; }
    [DetachProperty, SchemaIgnore]
    [Chunkable(31250)]
    public List<double> SurfacesValue
    {
      get
      {
        var list = new List<double>();
        if (Surfaces != null)
        {
          foreach (var srf in Surfaces)
          {
            list.AddRange(srf.ToList());
          }
        }
        return list;
      }
      set
      {
        if (value == null) return;
        var list = new List<Surface>();
        var done = false;
        var currentIndex = 0;
        while(!done)
        {
          var len = (int)value[currentIndex];
          list.Add(Surface.FromList(value.GetRange(currentIndex + 1, len)));
          currentIndex += len + 1;
          done = currentIndex >= value.Count;
        }
        Surfaces = list;
      }
    }

    /// <summary>
    /// Gets or sets the list of 3-dimensional curves in this <see cref="Brep"/> instance.
    /// </summary>
    [JsonIgnore]
    public List<ICurve> Curve3D { get; set; }
    [DetachProperty, SchemaIgnore]
    [Chunkable(31250)]
    public List<double> Curve3DValues
    {
      get
      {
        return CurveArrayEncodingExtensions.ToArray(Curve3D);
      }
      set
      {
        if (value != null)
          Curve3D = CurveArrayEncodingExtensions.FromArray(value);
      }
    }

    /// <summary>
    /// Gets or sets the list of 2-dimensional UV curves in this <see cref="Brep"/> instance.
    /// </summary>
    [JsonIgnore]
    public List<ICurve> Curve2D { get; set; }
    [DetachProperty, SchemaIgnore]
    [Chunkable(31250)]
    public List<double> Curve2DValues
    {
      get
      {
        return CurveArrayEncodingExtensions.ToArray(Curve2D);
      }
      set
      {
        if (value != null)
          Curve2D = CurveArrayEncodingExtensions.FromArray(value);
      }
    }

    /// <summary>
    /// Gets or sets the list of vertices in this <see cref="Brep"/> instance.
    /// </summary>
    [JsonIgnore]
    public List<Point> Vertices { get; set; }
    [DetachProperty, SchemaIgnore]
    [Chunkable(31250)]
    public List<double> VerticesValue
    {
      get
      {
        var list = new List<double>();
        list.Add(Units.GetEncodingFromUnit(units));
        foreach (var vertex in Vertices)
        {
          list.AddRange(vertex.ToList());
        }
        return list;
      }
      set
      {
        if (value != null)
        {
          var units = value.Count % 3 == 0 ? Units.None : Units.GetUnitFromEncoding(value[0]);
          for (int i = value.Count % 3 == 0 ? 0 : 1; i < value.Count; i += 3)
          {
            Vertices.Add(new Point(value[i], value[i + 1], value[i + 2], units));
          }
        }
      }
    }

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
    [JsonIgnore]
    public List<BrepTrim> Trims { get; set; }
    [DetachProperty, SchemaIgnore]
    [Chunkable(62500)]
    public List<int> TrimsValue
    {
      get
      {
        List<int> list = new List<int>();
        foreach(var trim in Trims)
        {
          list.Add(trim.EdgeIndex);
          list.Add(trim.StartIndex);
          list.Add(trim.EndIndex);
          list.Add(trim.FaceIndex);
          list.Add(trim.LoopIndex);
          list.Add(trim.CurveIndex);
          list.Add(trim.IsoStatus);
          list.Add((int)trim.TrimType);
          list.Add(trim.IsReversed ? 1 : 0);
        }
        return list;
      }
      set
      {
        if (value == null) return;
        var list = new List<BrepTrim>();
        for(int i = 0; i < value.Count; i+=9)
        {
          var trim = new BrepTrim()
          {
            EdgeIndex = value[i],
            StartIndex = value[i + 1],
            EndIndex = value[i + 2],
            FaceIndex = value[i + 3],
            LoopIndex = value[i + 4],
            CurveIndex = value[i + 5],
            IsoStatus = value[i + 6],
            TrimType = (BrepTrimType)value[i + 7],
            IsReversed = value[i + 8] == 1
          };
          list.Add(trim);
        }
        Trims = list;
      }
    }

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
    
    public Brep(string provenance, Mesh displayValue, string units = Units.Meters, string applicationId = null)
      : this(provenance, new List<Mesh>{displayValue}, units, applicationId)
    { }
    
    public Brep(string provenance, List<Mesh> displayValues, string units = Units.Meters, string applicationId = null) : this()
    {
      this.provenance = provenance;
      this.displayValue = displayValues;
      this.applicationId = applicationId;
      this.units = units;
    }


    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
      Surfaces.ForEach(s => s.units = units);
      
      for (var i = 0; i < Edges.Count; i++)
      {
        var e = Edges[i];
        lock (e)
          if (e.Brep != null)
          {
            e = new BrepEdge(this, e.Curve3dIndex, e.TrimIndices, e.StartIndex, e.Curve3dIndex, e.ProxyCurveIsReversed,
              e.Domain);
            Edges[i] = e;
          }
          else
            e.Brep = this;
      }
      
      for(var i = 0; i < Loops.Count; i++)
      {
        var l = Loops[i];
        lock(l)
          if (l.Brep != null)
          {
            l = new BrepLoop(this, l.FaceIndex, l.TrimIndices, l.Type);
            Loops[i] = l;
          }
          else
            l.Brep = this;
      }

      for (var i = 0; i < Trims.Count; i++)
      {
        var t = Trims[i];
        lock(t)
          if (t.Brep != null)
          {
            t = new BrepTrim(this, t.EdgeIndex, t.FaceIndex, t.LoopIndex, t.CurveIndex, t.IsoStatus, t.TrimType,
              t.IsReversed, t.StartIndex, t.EndIndex);
            Trims[i] = t;
          }
          else
            t.Brep = this;
      }

      for (var i = 0; i < Faces.Count; i++)
      {
        var f = Faces[i];
        lock(f)
          if (f.Brep != null)
          {
            f = new BrepFace(this, f.SurfaceIndex, f.LoopIndices, f.OuterLoopIndex, f.OrientationReversed);
            Faces[i] = f;
          }
          else
            f.Brep = this;
      }
    }

    /// <summary>
    /// Returns a new transformed Brep.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public bool TransformTo(Transform transform, out Brep brep)
    {
      var displayValues = new List<Mesh>(displayValue.Count);
      foreach (Mesh v in displayValue)
      {
        v.TransformTo(transform, out Mesh mesh);
        displayValues.Add(mesh);
      }

      var surfaces = new List<Surface>(Surfaces.Count);
      foreach ( var srf in Surfaces )
      {
        srf.TransformTo(transform, out var surface);
        surfaces.Add(surface);
      }

      brep = new Brep
      {
        provenance = provenance,
        units = units,
        displayValue = displayValues,
        Surfaces = surfaces,
        Curve3D = transform.ApplyToCurves(Curve3D, out bool success3D),
        Curve2D = transform.ApplyToCurves(Curve2D, out bool success2D),
        Vertices = transform.ApplyToPoints(Vertices),
        Edges = new List<BrepEdge>(Edges.Count),
        Loops = new List<BrepLoop>(Loops.Count),
        Trims = new List<BrepTrim>(Trims.Count),
        Faces = new List<BrepFace>(Faces.Count),
        IsClosed = IsClosed,
        Orientation = Orientation,
        applicationId = applicationId ?? id
      };

      foreach ( var e in Edges )
        brep.Edges.Add(new BrepEdge(brep, e.Curve3dIndex, e.TrimIndices, e.StartIndex, e.Curve3dIndex,
          e.ProxyCurveIsReversed,
          e.Domain));

      foreach ( var l in Loops )
        brep.Loops.Add(new BrepLoop(brep, l.FaceIndex, l.TrimIndices, l.Type));

      foreach ( var t in Trims )
        brep.Trims.Add(new BrepTrim(brep, t.EdgeIndex, t.FaceIndex, t.LoopIndex, t.CurveIndex, t.IsoStatus, t.TrimType,
          t.IsReversed, t.StartIndex, t.EndIndex));

      foreach ( var f in Faces )
        brep.Faces.Add(new BrepFace(brep, f.SurfaceIndex, f.LoopIndices, f.OuterLoopIndex, f.OrientationReversed));

      return success2D && success3D;
    }
    
    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> {value};
    }
    #endregion
  }

  /// <summary>
  /// Represents the orientation of a <see cref="Brep"/>
  /// </summary>
  public enum BrepOrientation
  {
    None = 0,
    Inward = -1,
    Outward = 1,
    Unknown = 2
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