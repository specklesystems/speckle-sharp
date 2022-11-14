using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using Speckle.Core.Models;
//using Objects;
//using Objects.BuiltElements.Archicad;
//using Objects.Geometry;
using OG = Objects.Geometry;

namespace ConverterRevitShared
{
  internal class DirectContext3DServer : IDirectContext3DServer
  {
    private Document document;

    private RenderingPassBufferStorage m_nonTransparentFaceBufferStorage;
    private RenderingPassBufferStorage m_transparentFaceBufferStorage;
    private RenderingPassBufferStorage m_edgeBufferStorage;
    private Guid m_guid;
    private Base speckleObj;

    public DirectContext3DServer(Base @base, Document doc)
    {
      m_guid = Guid.NewGuid();
      speckleObj = @base;
      document = doc;
    }
    public bool CanExecute(View dBView)
    {
      //if (!m_element.IsValidObject)
      //  return false;
      if (dBView.ViewType != ViewType.ThreeD)
        return false;

      Document doc = dBView.Document;
      Document otherDoc = document;
      return doc.Equals(otherDoc);
    }
    public string GetApplicationId() => " ";
    public Outline GetBoundingBox(View dBView)
    {
      return new Outline(new XYZ(0,0,0), new XYZ(1,1,1));
    }
    public string GetDescription() => "Implements preview functionality for a Speckle Object";
    public string GetName() => "Speckle Object Drawing Server";
    public Guid GetServerId() => m_guid;
    public ExternalServiceId GetServiceId() => ExternalServices.BuiltInExternalServices.DirectContext3DService;
    public string GetSourceId() => " ";
    public string GetVendorId() => "Speckle";
    public void RenderScene(View dBView, DisplayStyle displayStyle)
    {
      try
      {
        // Populate geometry buffers if they are not initialized or need updating.
        if (m_nonTransparentFaceBufferStorage == null || m_nonTransparentFaceBufferStorage.needsUpdate(displayStyle) ||
            m_transparentFaceBufferStorage == null || m_transparentFaceBufferStorage.needsUpdate(displayStyle) ||
            m_edgeBufferStorage == null || m_edgeBufferStorage.needsUpdate(displayStyle))
        {
          //Options options = new Options();
          //GeometryElement geomElem = m_element.get_Geometry(options);

          CreateBufferStorageForBase(speckleObj, displayStyle);
          //CreateBufferStorageForMesh(CreateRhinoStylePolygon(), displayStyle);
        }

        // Submit a subset of the geometry for drawing. Determine what geometry should be submitted based on
        // the type of the rendering pass (opaque or transparent) and DisplayStyle (wireframe or shaded).

        // If the server is requested to submit transparent geometry, DrawContext().IsTransparentPass()
        // will indicate that the current rendering pass is for transparent objects.
        RenderingPassBufferStorage faceBufferStorage = DrawContext.IsTransparentPass() ? m_transparentFaceBufferStorage : m_nonTransparentFaceBufferStorage;

        // Conditionally submit triangle primitives (for non-wireframe views).
        if (displayStyle != DisplayStyle.Wireframe &&
            faceBufferStorage.PrimitiveCount > 0)
          DrawContext.FlushBuffer(faceBufferStorage.VertexBuffer,
                                  faceBufferStorage.VertexBufferCount,
                                  faceBufferStorage.IndexBuffer,
                                  faceBufferStorage.IndexBufferCount,
                                  faceBufferStorage.VertexFormat,
                                  faceBufferStorage.EffectInstance, PrimitiveType.TriangleList, 0,
                                  faceBufferStorage.PrimitiveCount);

        // Conditionally submit line segment primitives.
        if (displayStyle != DisplayStyle.Shading &&
            m_edgeBufferStorage.PrimitiveCount > 0)
          DrawContext.FlushBuffer(m_edgeBufferStorage.VertexBuffer,
                                  m_edgeBufferStorage.VertexBufferCount,
                                  m_edgeBufferStorage.IndexBuffer,
                                  m_edgeBufferStorage.IndexBufferCount,
                                  m_edgeBufferStorage.VertexFormat,
                                  m_edgeBufferStorage.EffectInstance, PrimitiveType.LineList, 0,
                                  m_edgeBufferStorage.PrimitiveCount);
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.WriteLine(e.ToString());
      }
    }
    public bool UseInTransparentPass(View dBView) => true;
    public bool UsesHandles() => false;

    private void CreateBufferStorageForBase(Base @base, DisplayStyle displayStyle)
    {
      var meshes = @base.GetType().GetProperty("displayValue").GetValue(@base) as List<OG.Mesh>;
      foreach (var mesh in meshes)
        CreateBufferStorageForMesh(mesh, displayStyle);
    }

    // Initialize and populate buffers that hold graphics primitives, set up related parameters that are needed for drawing.
    private void CreateBufferStorageForMesh(OG.Mesh mesh, DisplayStyle displayStyle)
    {
      //List<Solid> allSolids = new List<Solid>();

      //foreach (GeometryObject geomObj in geomElem)
      //{
      //  if (geomObj is Solid)
      //  {
      //    Solid solid = (Solid)geomObj;
      //    if (solid.Volume > 1e-06)
      //      allSolids.Add(solid);
      //  }
      //}

      m_nonTransparentFaceBufferStorage = new RenderingPassBufferStorage(displayStyle);
      m_transparentFaceBufferStorage = new RenderingPassBufferStorage(displayStyle);
      m_edgeBufferStorage = new RenderingPassBufferStorage(displayStyle);

      // Collect primitives (and associated rendering parameters, such as colors) from faces and edges.
      //foreach (Face face in mesh.faces)
      //{
      //  if (face.Area > 1e-06)
      //  {
      //    Mesh mesh = face.Triangulate();

      //    ElementId materialId = face.MaterialElementId;
      //    bool isTransparent = false;
      //    ColorWithTransparency cwt = new ColorWithTransparency(127, 127, 127, 0);
      //    if (materialId != ElementId.InvalidElementId)
      //    {
      //      Material material = m_element.Document.GetElement(materialId) as Material;

      //      Color color = material.Color;
      //      int transparency0To100 = material.Transparency;
      //      uint transparency0To255 = (uint)((float)transparency0To100 / 100f * 255f);

      //      cwt = new ColorWithTransparency(color.Red, color.Green, color.Blue, transparency0To255);
      //      if (transparency0To255 > 0)
      //      {
      //        isTransparent = true;
      //      }
      //    }

      //    BoundingBoxUV env = face.GetBoundingBox();
      //    UV center = 0.5 * (env.Min + env.Max);
      //    XYZ normal = face.ComputeNormal(center);

      //    SpeckleMeshInfo meshInfo = new MeshInfo(mesh, normal, cwt);

      //    if (isTransparent)
      //    {
      //      m_transparentFaceBufferStorage.Meshes.Add(meshInfo);
      //      m_transparentFaceBufferStorage.VertexBufferCount += mesh.Vertices.Count;
      //      m_transparentFaceBufferStorage.PrimitiveCount += mesh.NumTriangles;
      //    }
      //    else
      //    {
      //      m_nonTransparentFaceBufferStorage.Meshes.Add(meshInfo);
      //      m_nonTransparentFaceBufferStorage.VertexBufferCount += mesh.Vertices.Count;
      //      m_nonTransparentFaceBufferStorage.PrimitiveCount += mesh.NumTriangles;
      //    }
      //  }
      //}

      var meshInfo = new SpeckleMeshInfo(mesh);
      m_nonTransparentFaceBufferStorage.Meshes.Add(meshInfo);
      m_nonTransparentFaceBufferStorage.VertexBufferCount += mesh.VerticesCount;
      m_nonTransparentFaceBufferStorage.PrimitiveCount += meshInfo.Faces.Count;

      //m_edgeBufferStorage.VertexBufferCount += mesh.ed;
      //m_edgeBufferStorage.PrimitiveCount += xyzs.Count - 1;
      //m_edgeBufferStorage.EdgeXYZs.Add(xyzs);

      //foreach (Edge edge in solid.Edges)
      //{
      //  // if (edge.Length > 1e-06)
      //  {
      //    IList<XYZ> xyzs = edge.Tessellate();

      //    m_edgeBufferStorage.VertexBufferCount += xyzs.Count;
      //    m_edgeBufferStorage.PrimitiveCount += xyzs.Count - 1;
      //    m_edgeBufferStorage.EdgeXYZs.Add(xyzs);
      //  }
      //}

      // Fill out buffers with primitives based on the intermediate information about faces and edges.
      ProcessFaces(m_nonTransparentFaceBufferStorage);
      //ProcessFaces(m_transparentFaceBufferStorage);
      //ProcessEdges(m_edgeBufferStorage);
    }

    private void ProcessFaces(RenderingPassBufferStorage bufferStorage)
    {
      List<SpeckleMeshInfo> meshes = bufferStorage.Meshes;
      List<int> numVerticesInMeshesBefore = new List<int>();
      if (meshes.Count == 0) return;

      bool useNormals = bufferStorage.DisplayStyle == DisplayStyle.Shading ||
         bufferStorage.DisplayStyle == DisplayStyle.ShadingWithEdges;

      // Vertex attributes are stored sequentially in vertex buffers. The attributes can include position, normal vector, and color.
      // All vertices within a vertex buffer must have the same format. Possible formats are enumerated by VertexFormatBits.
      // Vertex format also determines the type of rendering effect that can be used with the vertex buffer. In this sample,
      // the color is always encoded in the vertex attributes.

      bufferStorage.FormatBits = useNormals ? VertexFormatBits.PositionNormalColored : VertexFormatBits.PositionColored;

      // The format of the vertices determines the size of the vertex buffer.
      int vertexBufferSizeInFloats = (useNormals ? VertexPositionNormalColored.GetSizeInFloats() : VertexPositionColored.GetSizeInFloats()) *
         bufferStorage.VertexBufferCount;
      numVerticesInMeshesBefore.Add(0);

      bufferStorage.VertexBuffer = new VertexBuffer(vertexBufferSizeInFloats);
      bufferStorage.VertexBuffer.Map(vertexBufferSizeInFloats);

      if (useNormals)
      {
        // A VertexStream is used to write data into a VertexBuffer.
        VertexStreamPositionNormalColored vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPositionNormalColored();
        foreach (SpeckleMeshInfo meshInfo in meshes)
        {
          OG.Mesh mesh = meshInfo.Mesh;
          foreach (OG.Point vertex in mesh.GetPoints())
          {
            vertexStream.AddVertex(new VertexPositionNormalColored(new XYZ(vertex.x, vertex.y, vertex.z), meshInfo.Normal, meshInfo.ColorWithTransparency));
          }

          numVerticesInMeshesBefore.Add(numVerticesInMeshesBefore.Last() + mesh.VerticesCount);
        }
      }
      else
      {
        // A VertexStream is used to write data into a VertexBuffer.
        VertexStreamPositionColored vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPositionColored();
        foreach (SpeckleMeshInfo meshInfo in meshes)
        {
          OG.Mesh mesh = meshInfo.Mesh;
          // make the color of all faces white in HLR
          //ColorWithTransparency color = (bufferStorage.DisplayStyle == DisplayStyle.HLR) ?
          //   new ColorWithTransparency(255, 255, 255, meshInfo.ColorWithTransparency.GetTransparency()) :
          //   meshInfo.ColorWithTransparency;
          var color = new ColorWithTransparency(255, 255, 255, 0);
          foreach (OG.Point vertex in mesh.GetPoints())
          {
            vertexStream.AddVertex(new VertexPositionColored(new XYZ(vertex.x, vertex.y, vertex.z), color));
          }

          numVerticesInMeshesBefore.Add(numVerticesInMeshesBefore.Last() + mesh.VerticesCount);
        }
      }

      bufferStorage.VertexBuffer.Unmap();

      // Primitives are specified using a pair of vertex and index buffers. An index buffer contains a sequence of indices into
      // the associated vertex buffer, each index referencing a particular vertex.

      int meshNumber = 0;
      bufferStorage.IndexBufferCount = bufferStorage.PrimitiveCount * IndexTriangle.GetSizeInShortInts();
      int indexBufferSizeInShortInts = 1 * bufferStorage.IndexBufferCount;
      bufferStorage.IndexBuffer = new IndexBuffer(indexBufferSizeInShortInts);
      bufferStorage.IndexBuffer.Map(indexBufferSizeInShortInts);
      {
        // An IndexStream is used to write data into an IndexBuffer.
        IndexStreamTriangle indexStream = bufferStorage.IndexBuffer.GetIndexStreamTriangle();
        foreach (SpeckleMeshInfo meshInfo in meshes)
        {
          var mesh = meshInfo.Mesh;
          int startIndex = numVerticesInMeshesBefore[meshNumber];
          foreach (var face in meshInfo.Faces)
          {
            // Add three indices that define a triangle.
            indexStream.AddTriangle(new IndexTriangle(startIndex + face[0],
                                                      startIndex + face[1],
                                                      startIndex + face[2]));
          }
          meshNumber++;
        }
      }
      bufferStorage.IndexBuffer.Unmap();


      // VertexFormat is a specification of the data that is associated with a vertex (e.g., position).
      bufferStorage.VertexFormat = new VertexFormat(bufferStorage.FormatBits);
      // Effect instance is a specification of the appearance of geometry. For example, it may be used to specify color, if there is no color information provided with the vertices.
      bufferStorage.EffectInstance = new EffectInstance(bufferStorage.FormatBits);
    }

    // A helper function, analogous to ProcessFaces.
    private void ProcessEdges(RenderingPassBufferStorage bufferStorage)
    {
      List<IList<XYZ>> edges = bufferStorage.EdgeXYZs;
      if (edges.Count == 0)
        return;

      // Edges are encoded as line segment primitives whose vertices contain only position information.
      bufferStorage.FormatBits = VertexFormatBits.Position;

      int edgeVertexBufferSizeInFloats = VertexPosition.GetSizeInFloats() * bufferStorage.VertexBufferCount;
      List<int> numVerticesInEdgesBefore = new List<int>();
      numVerticesInEdgesBefore.Add(0);

      bufferStorage.VertexBuffer = new VertexBuffer(edgeVertexBufferSizeInFloats);
      bufferStorage.VertexBuffer.Map(edgeVertexBufferSizeInFloats);
      {
        VertexStreamPosition vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPosition();
        foreach (IList<XYZ> xyzs in edges)
        {
          foreach (XYZ vertex in xyzs)
          {
            vertexStream.AddVertex(new VertexPosition(vertex));
          }

          numVerticesInEdgesBefore.Add(numVerticesInEdgesBefore.Last() + xyzs.Count);
        }
      }
      bufferStorage.VertexBuffer.Unmap();

      int edgeNumber = 0;
      bufferStorage.IndexBufferCount = bufferStorage.PrimitiveCount * IndexLine.GetSizeInShortInts();
      int indexBufferSizeInShortInts = 1 * bufferStorage.IndexBufferCount;
      bufferStorage.IndexBuffer = new IndexBuffer(indexBufferSizeInShortInts);
      bufferStorage.IndexBuffer.Map(indexBufferSizeInShortInts);
      {
        IndexStreamLine indexStream = bufferStorage.IndexBuffer.GetIndexStreamLine();
        foreach (IList<XYZ> xyzs in edges)
        {
          int startIndex = numVerticesInEdgesBefore[edgeNumber];
          for (int i = 1; i < xyzs.Count; i++)
          {
            // Add two indices that define a line segment.
            indexStream.AddLine(new IndexLine((int)(startIndex + i - 1),
                                              (int)(startIndex + i)));
          }
          edgeNumber++;
        }
      }
      bufferStorage.IndexBuffer.Unmap();


      bufferStorage.VertexFormat = new VertexFormat(bufferStorage.FormatBits);
      bufferStorage.EffectInstance = new EffectInstance(bufferStorage.FormatBits);
    }

    #region Helper classes

    // A container to hold information associated with a triangulated face.
    class MeshInfo
    {
      public MeshInfo(OG.Mesh mesh, XYZ normal, ColorWithTransparency color)
      {
        Mesh = mesh;
        Normal = normal;
        ColorWithTransparency = color;
      }

      public OG.Mesh Mesh;
      public XYZ Normal;
      public ColorWithTransparency ColorWithTransparency;
    }

    // A class that brings together all the data and rendering parameters that are needed to draw one sequence of primitives (e.g., triangles)
    // with the same format and appearance.
    class RenderingPassBufferStorage
    {
      public RenderingPassBufferStorage(DisplayStyle displayStyle)
      {
        DisplayStyle = displayStyle;
        Meshes = new List<SpeckleMeshInfo>();
        EdgeXYZs = new List<IList<XYZ>>();
      }

      public bool needsUpdate(DisplayStyle newDisplayStyle)
      {
        if (newDisplayStyle != DisplayStyle)
          return true;

        if (PrimitiveCount > 0)
          if (VertexBuffer == null || !VertexBuffer.IsValid() ||
              IndexBuffer == null || !IndexBuffer.IsValid() ||
              VertexFormat == null || !VertexFormat.IsValid() ||
              EffectInstance == null || !EffectInstance.IsValid())
            return true;

        return false;
      }

      public DisplayStyle DisplayStyle { get; set; }

      public VertexFormatBits FormatBits { get; set; }

      public List<SpeckleMeshInfo> Meshes { get; set; }
      public List<IList<XYZ>> EdgeXYZs { get; set; }

      public int PrimitiveCount { get; set; }
      public int VertexBufferCount { get; set; }
      public int IndexBufferCount { get; set; }
      public VertexBuffer VertexBuffer { get; set; }
      public IndexBuffer IndexBuffer { get; set; }
      public VertexFormat VertexFormat { get; set; }
      public EffectInstance EffectInstance { get; set; }
    }

    public class SpeckleMeshInfo
    {
      public OG.Mesh Mesh;
      public List<OG.Point> Vertices;
      public List<int[]> Faces;
      public XYZ Normal;
      public ColorWithTransparency ColorWithTransparency;
      //public ConcurrentDictionary<Tuple<int, int>, ConcurrentBag<int>> EdgeFaceConnection;

      public SpeckleMeshInfo(OG.Mesh mesh)
      {
        //var result = new ConcurrentDictionary<Tuple<int, int>, ConcurrentBag<int>>();
        Mesh = mesh;
        Faces = GetFaceIndices(mesh).ToList();
        Vertices = mesh.GetPoints();
        //var faceIndex = 0;
        //foreach (var indices in faces)
        //{
        //  for (var j = 0; j < indices.Length; j++)
        //  {
        //    var iA = indices[j];
        //    var iB = indices[(j + 1) % indices.Length];
        //    var temp = iA;
        //    iA = temp < iB ? iA : iB;
        //    iB = temp < iB ? iB : temp;
        //    //var connectedFaces = result.GetOrAdd(new Tuple<int, int>(iA, iB), new ConcurrentBag<int>());
        //    //connectedFaces.Add(faceIndex);
        //  }
        //  faceIndex++;
        //}
      }
      public static IEnumerable<int[]> GetFaceIndices(OG.Mesh mesh)
      {
        var i = 0;
        while (i < mesh.faces.Count)
        {
          var n = mesh.faces[i];
          if (n < 3) n += 3; // 0 -> 3, 1 -> 4 to preserve backwards compatibility

          var points = mesh.faces.GetRange(i + 1, n).ToArray();
          yield return points;
          i += n + 1;
        }
      }
    }

    private static OG.Mesh CreateRhinoStylePolygon()
    {
      return new OG.Mesh()
      {
        vertices =
                {
                    0, 0, 0,
                    0, 0, 1,
                    1, 0, 1,
                    0, 0, 0,
                    1, 0, 1,
                    1, 0, 0
                },
        faces =
                {
                    3, 0, 1, 2,
                    3, 3, 4, 5
                },
        textureCoordinates =
                {
                    0,0,
                    0,1,
                    1,1,
                    0,0,
                    1,1,
                    1,0
                },
      };
    }

    #endregion
  }
}
