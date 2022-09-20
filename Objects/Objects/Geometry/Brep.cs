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
    [JsonIgnore]
    public List<BrepEdge> Edges { get; set; }
    
    [DetachProperty, SchemaIgnore]
    [Chunkable(62500)]
    public List<double?> EdgesValue
    {
      get
      {
        return Edges.SelectMany(e =>
        {
          var ints = new List<double?>();
          ints.Add(e.Curve3dIndex);
          ints.Add(e.StartIndex);
          ints.Add(e.EndIndex);
          ints.Add(Convert.ToInt32(e.ProxyCurveIsReversed));
          ints.Add(e.Domain.start);
          ints.Add(e.Domain.end);
          ints.AddRange(e.TrimIndices.Select(Convert.ToDouble).Cast<double?>());
          return ints.Prepend(ints.Count);
        }).ToList();
      }
      set
      {
        Edges = new List<BrepEdge>();
        if (value == null || value.Count == 0) return;
        var i = 0;
        while (i < value.Count)
        {
          int n = Convert.ToInt32(value[i]);

          var loopValues = value.GetRange(i + 1, n);
          var curve3dIndex = Convert.ToInt32(loopValues[0]);
          var startIndex = Convert.ToInt32(loopValues[1]);
          var endIndex = Convert.ToInt32(loopValues[2]);
          var proxyReversed = Convert.ToBoolean(loopValues[3]);
          var domainStart = loopValues[4];
          var domainEnd = loopValues[5];
          Interval domain = null;
          if (domainStart.HasValue && domainEnd.HasValue)
          {
            domain = new Interval(domainStart.Value, domainEnd.Value);
          }
          var trimIndices = loopValues
            .GetRange(6, loopValues.Count - 6)
            .Select(d => Convert.ToInt32(d))
            .ToArray();
          
          var edge = new BrepEdge(this, curve3dIndex, trimIndices, startIndex, endIndex, proxyReversed, domain);
          Edges.Add(edge);
          i += n + 1;
        }
      }
    }

    /// <summary>
    /// Gets or sets the list of closed UV loops in this <see cref="Brep"/> instance.
    /// </summary>
    [JsonIgnore]
    public List<BrepLoop> Loops { get; set; }
    
    [DetachProperty, SchemaIgnore]
    [Chunkable(62500)]
    public List<int> LoopsValue
    {
      get
      {
        return Loops.SelectMany(l =>
        {
          var ints = new List<int>();
          ints.Add(l.FaceIndex);
          ints.Add((int)l.Type);
          ints.AddRange(l.TrimIndices);
          return ints.Prepend(ints.Count);
        }).ToList();
      }
      set
      {
        Loops = new List<BrepLoop>();
        if (value == null || value.Count == 0) return;
        var i = 0;
        while (i < value.Count)
        {
          int n = value[i];

          var loopValues = value.GetRange(i + 1, n);
          var faceIndex = loopValues[0];
          var type = (BrepLoopType)loopValues[1];
          var trimIndices = loopValues.GetRange(2, loopValues.Count - 2);
          var loop = new BrepLoop(this, faceIndex, trimIndices, type);
          Loops.Add(loop);
          i += n + 1;
        }
      }
    }

    
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
          var trim = new BrepTrim
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
    [JsonIgnore]
    public List<BrepFace> Faces { get; set; }

    [DetachProperty, SchemaIgnore]
    [Chunkable(62500)]
    public List<int> FacesValue
    {
      get
      {
        return Faces.SelectMany(f =>
        {
          var ints = new List<int>();
          ints.Add(f.SurfaceIndex);
          ints.Add(f.OuterLoopIndex);
          ints.Add(f.OrientationReversed ? 1 : 0);
          ints.AddRange(f.LoopIndices);
          return ints.Prepend(ints.Count);
        }).ToList();
      }
      set
      {
        Faces = new List<BrepFace>();
        if (value == null || value.Count == 0) return;
        var i = 0;
        while (i < value.Count)
        {
          int n = value[i];

          var faceValues = value.GetRange(i + 1, n);
          var surfIndex = faceValues[0];
          var outerLoopIndex = faceValues[1];
          var orientationIsReversed = faceValues[2] == 1;
          var loopIndices = faceValues.GetRange(3, faceValues.Count - 3);
          var face = new BrepFace(this, surfIndex, loopIndices, outerLoopIndex, orientationIsReversed);
          Faces.Add(face);
          i += n + 1;
        }
      }
    }

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
            e = new BrepEdge(this, e.Curve3dIndex, e.TrimIndices, e.StartIndex, e.EndIndex, e.ProxyCurveIsReversed,
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
        srf.TransformTo(transform, out Surface surface);
        surfaces.Add(surface);
      }

      brep = new Brep
      {
        provenance = provenance,
        units = units,
        displayValue = displayValues,
        Surfaces = surfaces,
        Curve3D = transform.ApplyToCurves(Curve3D, out bool success3D),
        Curve2D = new List<ICurve>(Curve2D),
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
        brep.Edges.Add(new BrepEdge(brep, e.Curve3dIndex, e.TrimIndices, e.StartIndex, e.EndIndex,
          e.ProxyCurveIsReversed,
          e.Domain));

      foreach ( var l in Loops )
        brep.Loops.Add(new BrepLoop(brep, l.FaceIndex, l.TrimIndices, l.Type));

      foreach ( var t in Trims )
        brep.Trims.Add(new BrepTrim(brep, t.EdgeIndex, t.FaceIndex, t.LoopIndex, t.CurveIndex, t.IsoStatus, t.TrimType,
          t.IsReversed, t.StartIndex, t.EndIndex));

      foreach ( var f in Faces )
        brep.Faces.Add(new BrepFace(brep, f.SurfaceIndex, f.LoopIndices, f.OuterLoopIndex, f.OrientationReversed));

      return success3D;
    }
    
    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> {value};
    }
    #endregion

    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      var res = TransformTo(transform, out Brep brep);
      transformed = brep;
      return res;
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
    Unknown = 2
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