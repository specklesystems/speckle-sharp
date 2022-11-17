using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using Objects.Converter.Revit;
using Speckle.Core.Models;
using OG = Objects.Geometry;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public static ConverterRevit Instance;
    public class DirectContext3DServer : IDirectContext3DServer
    {
      private Document document;

      private RenderingPassBufferStorage m_nonTransparentFaceBufferStorage;
      private RenderingPassBufferStorage m_transparentFaceBufferStorage;
      private RenderingPassBufferStorage m_edgeBufferStorage;
      private Guid m_guid;
      private IEnumerable<OG.Mesh> speckleMeshes;
      public OG.Point minValues = new OG.Point(double.MaxValue, double.MaxValue, double.MaxValue);
      public OG.Point maxValues = new OG.Point(double.MinValue, double.MinValue, double.MinValue);
      public bool allMeshesStored => speckleMeshes.Count() == m_nonTransparentFaceBufferStorage?.Meshes.Count + m_transparentFaceBufferStorage?.Meshes.Count;
      public DirectContext3DServer(IEnumerable<OG.Mesh> meshes, Document doc)
      {
        m_guid = Guid.NewGuid();
        speckleMeshes = meshes;
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
        //return new Outline(new XYZ(0, 0, 0), new XYZ(2, 2, 2));

        return new Outline
          (
            new XYZ(minValues.x, minValues.y, minValues.z),
            new XYZ(maxValues.x, maxValues.y, maxValues.z)
          );
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
          if (m_nonTransparentFaceBufferStorage == null || m_nonTransparentFaceBufferStorage.NeedsUpdate(displayStyle) ||
              m_transparentFaceBufferStorage == null || m_transparentFaceBufferStorage.NeedsUpdate(displayStyle) ||
              m_edgeBufferStorage == null || m_edgeBufferStorage.NeedsUpdate(displayStyle))
          {

            CreateBufferStorageForMeshes(displayStyle);
          }

          // Submit a subset of the geometry for drawing. Determine what geometry should be submitted based on
          // the type of the rendering pass (opaque or transparent) and DisplayStyle (wireframe or shaded).

          // If the server is requested to submit transparent geometry, DrawContext().IsTransparentPass()
          // will indicate that the current rendering pass is for transparent objects.
          RenderingPassBufferStorage faceBufferStorage = DrawContext.IsTransparentPass() ? m_transparentFaceBufferStorage : m_nonTransparentFaceBufferStorage;

          // Conditionally submit triangle primitives (for non-wireframe views).
          if (displayStyle != DisplayStyle.Wireframe &&
              faceBufferStorage?.PrimitiveCount > 0)
            DrawContext.FlushBuffer(faceBufferStorage.VertexBuffer,
                                    faceBufferStorage.VertexBufferCount,
                                    faceBufferStorage.IndexBuffer,
                                    faceBufferStorage.IndexBufferCount,
                                    faceBufferStorage.VertexFormat,
                                    faceBufferStorage.EffectInstance, PrimitiveType.TriangleList, 0,
                                    faceBufferStorage.PrimitiveCount);

          // Conditionally submit line segment primitives.
          if (displayStyle == DisplayStyle.Wireframe &&
            displayStyle != DisplayStyle.Shading &&
            m_edgeBufferStorage?.PrimitiveCount > 0)
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

      private void CreateBufferStorageForMeshes(DisplayStyle displayStyle)
      {
        if (allMeshesStored)
          RefreshBufferStorage(displayStyle);
        else
          foreach (var mesh in speckleMeshes)
            CreateBufferStorageForMesh(mesh, displayStyle);
      }

      private void RefreshBufferStorage(DisplayStyle displayStyle)
      {
        m_nonTransparentFaceBufferStorage.DisplayStyle = displayStyle;
        m_transparentFaceBufferStorage.DisplayStyle = displayStyle;
        m_edgeBufferStorage.DisplayStyle = displayStyle;

        // Fill out buffers with primitives based on the intermediate information about faces and edges.
        ProcessFaces(m_nonTransparentFaceBufferStorage);
        ProcessFaces(m_transparentFaceBufferStorage);
        if (displayStyle == DisplayStyle.Wireframe)
          ProcessEdges(m_edgeBufferStorage);
      }

      // Initialize and populate buffers that hold graphics primitives, set up related parameters that are needed for drawing.
      private void CreateBufferStorageForMesh(OG.Mesh mesh, DisplayStyle displayStyle)
      {
        ColorWithTransparency color = null;
        if (mesh["renderMaterial"] is Objects.Other.RenderMaterial mat)
        {
          // System.Drawing.Color treats the A value as opacity, with 255 being fully opaque.
          // Revit interprets the A value as transparency, with 0 being fully opaque and 255 being fully transparent
          // so we need to translate the A value as below
          color = new ColorWithTransparency(mat.diffuseColor.R, mat.diffuseColor.G, mat.diffuseColor.B, Math.Max((uint)(255 - mat.diffuseColor.A * mat.opacity), 0));
        }

        color ??= new ColorWithTransparency(255, 255, 255, 0);
        var meshInfo = new SpeckleMeshInfo(mesh, color, ref minValues, ref maxValues);

        m_nonTransparentFaceBufferStorage ??= new RenderingPassBufferStorage(displayStyle);
        m_transparentFaceBufferStorage ??= new RenderingPassBufferStorage(displayStyle);
        m_edgeBufferStorage ??= new RenderingPassBufferStorage(displayStyle);

        if (color.GetTransparency() != 0)
        {
          m_transparentFaceBufferStorage.Meshes.Add(meshInfo);
          m_transparentFaceBufferStorage.VertexBufferCount += meshInfo.Mesh.VerticesCount;
          m_transparentFaceBufferStorage.PrimitiveCount += meshInfo.Faces.Count;
        }
        else
        {
          m_nonTransparentFaceBufferStorage.Meshes.Add(meshInfo);
          m_nonTransparentFaceBufferStorage.VertexBufferCount += meshInfo.Mesh.VerticesCount;
          m_nonTransparentFaceBufferStorage.PrimitiveCount += meshInfo.Faces.Count;
        }

        foreach (var xyzs in meshInfo.XYZs)
        {
          m_edgeBufferStorage.VertexBufferCount += xyzs.Count;
          m_edgeBufferStorage.PrimitiveCount += xyzs.Count - 1;
          m_edgeBufferStorage.EdgeXYZs.Add(xyzs);
        }

        RefreshBufferStorage(displayStyle);
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

        if (useNormals) //must use normals for shaded displayStyle
        {
          // A VertexStream is used to write data into a VertexBuffer.
          VertexStreamPositionNormalColored vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPositionNormalColored();
          foreach (SpeckleMeshInfo meshInfo in meshes)
          {
            OG.Mesh mesh = meshInfo.Mesh;
            var addedVertexIndicies = new List<int>();
            for (var i = 0; i < meshInfo.Faces.Count; i++)
            {
              foreach (var index in meshInfo.Faces[i])
              {
                if (addedVertexIndicies.Contains(index))
                  continue;

                var p1 = meshInfo.Vertices.ElementAt(index);
                vertexStream.AddVertex(new VertexPositionNormalColored(p1, meshInfo.Normals.ElementAt(i), meshInfo.ColorWithTransparency));
                addedVertexIndicies.Add(index);
              }
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

            foreach (XYZ vertex in meshInfo.Vertices)
              vertexStream.AddVertex(new VertexPositionColored(vertex, meshInfo.ColorWithTransparency));

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

        public bool NeedsUpdate(DisplayStyle newDisplayStyle)
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
        public List<XYZ> Vertices = new List<XYZ>();
        public List<int[]> Faces;
        public List<List<XYZ>> XYZs = new List<List<XYZ>>();
        public List<XYZ> Normals = new List<XYZ>();
        public ColorWithTransparency ColorWithTransparency;

        public SpeckleMeshInfo(OG.Mesh mesh, ColorWithTransparency color, ref OG.Point minValues, ref OG.Point maxValue)
        {
          Mesh = mesh;
          ColorWithTransparency = color;
          Faces = GetFaceIndices(mesh).ToList();

          foreach(var vertex in mesh.GetPoints())
            Vertices.Add(Instance.PointToNative(vertex));

          var edges = new List<(int, int)>();
          var faceIndex = 0;
          foreach (var indices in Faces)
          {
            var vectorA = Vertices.ElementAt(indices[0]);
            var vectorB = Vertices.ElementAt(indices[1]);
            var vectorC = Vertices.ElementAt(indices[2]);
            var result = (vectorB - vectorA).CrossProduct(vectorC - vectorA).Normalize();

            try
            {
              Normals.Add(result);
            }
            catch (Exception ex)
            {
              Normals.Add(new XYZ(0, 0, 1));
            }

            for (var j = 0; j < indices.Length; j++)
            {
              var iA = indices[j];
              var iB = indices[(j + 1) % indices.Length];
              var temp = iA;
              iA = temp < iB ? iA : iB;
              iB = temp < iB ? iB : temp;

              if (edges.Contains((iA, iB)))
                continue;

              edges.Add((iA, iB));
              var p1 = Vertices.ElementAt(iA);
              var p2 = Vertices.ElementAt(iB);
              XYZs.Add(new List<XYZ> { p1, p2 });

              minValues.x = Math.Min(minValues.x, Math.Min(p1.X, p2.X));
              minValues.y = Math.Min(minValues.y, Math.Min(p1.Y, p2.Y));
              minValues.z = Math.Min(minValues.z, Math.Min(p1.Z, p2.Z));

              maxValue.x = Math.Max(maxValue.x, Math.Max(p1.X, p2.X));
              maxValue.y = Math.Max(maxValue.y, Math.Max(p1.Y, p2.Y));
              maxValue.z = Math.Max(maxValue.z, Math.Max(p1.Z, p2.Z));
            }
            faceIndex++;
          }
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

      #endregion
    }
  }
}
