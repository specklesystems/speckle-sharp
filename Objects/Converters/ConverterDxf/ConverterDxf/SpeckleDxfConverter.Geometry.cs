using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.netDxf.Units;
using Dxf = Speckle.netDxf;
using Dxfe = Speckle.netDxf.Entities;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Point = Objects.Geometry.Point;

namespace Objects.Converters.DxfConverter
{
    public partial class SpeckleDxfConverter
    {
        public Dxf.Vector3 VectorToNative(Point pt) => VectorToNative(new Vector(pt) { units = pt.units });
        public Dxf.Vector3 VectorToNative(Vector pt) => new Dxf.Vector3(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
        public Dxf.Entities.Point PointToNative(Point pt) => new Dxf.Entities.Point(VectorToNative(pt));

        public Dxf.Entities.Line LineToNative(Line line) =>
            new Dxf.Entities.Line(VectorToNative(line.start), VectorToNative(line.end));

        public Dxfe.Mesh MeshToNative(Mesh mesh) => new Dxfe.Mesh(
            mesh.GetPoints().Select(VectorToNative),
            mesh.GetFaceIndices()
        )
        {
            Color = MaterialToNativeColor(mesh["renderMaterial"] as RenderMaterial)
        };

        public IEnumerable<Dxfe.EntityObject> MeshToNativePretty(Mesh mesh, RenderMaterial renderMaterial = null)
        {
            var topology = mesh.GetMeshEdgeFaces();
            return MeshFacesToNative(
                topology,
                renderMaterial: renderMaterial ?? mesh["renderMaterial"] as RenderMaterial
                ).Concat<Dxfe.EntityObject>(MeshEdgesToNative(topology));
        }

        private IEnumerable<Dxfe.Line> MeshEdgesToNative(MeshTopologyResult topology,
                                                         string edgeLayerName = "Mesh boundaries") =>
            topology.EdgeFaceConnection
                    .Where((kv) => kv.Value.Count == 1)
                    .Select(kv =>
                     {
                         var (iA, iB) = kv.Key;
                         var line = new Dxfe.Line(
                             VectorToNative(topology.Vertices[iA]),
                             VectorToNative(topology.Vertices[iB]));
                         line.Layer = new Dxf.Tables.Layer(edgeLayerName);
                         return line;
                     });

        private IEnumerable<Dxfe.Face3D> MeshFacesToNative(MeshTopologyResult topology, string layerName = "Mesh Faces", RenderMaterial renderMaterial = null)
        {
            var vertices = topology.Vertices.Select(VectorToNative).ToList();
            return topology.Faces.Select(indices =>
            {
                var points = indices.Select(i => vertices[i]).ToList();
                Dxfe.Face3D face;
                switch (points.Count)
                {
                    case 3:
                        face = new Dxfe.Face3D(points[0], points[1], points[2]);
                        break;
                    case 4:
                        face = new Dxfe.Face3D(points[0], points[1], points[2], points[3]);
                        break;
                    default:
                        Report.Log("Ignoring n-gon face, currently unsupported.");
                        return null;
                }

                face.EdgeFlags = (Dxfe.Face3DEdgeFlags)15; // All edges hidden!
                face.Layer = new Dxf.Tables.Layer(layerName);
                face.Color = MaterialToNativeColor(renderMaterial);
                return face;
            }).Where(f => f != null);
        }

        public Dxf.AciColor MaterialToNativeColor(RenderMaterial renderMaterial)
        {
            if(renderMaterial == null) return Dxf.AciColor.ByLayer;
            var sysColor = System.Drawing.Color.FromArgb(renderMaterial.diffuse);
            return new Dxf.AciColor(sysColor.R, sysColor.G, sysColor.B);
        }

        public IEnumerable<Dxfe.EntityObject> BrepToNative(Brep brep)
        {
            return Settings.PrettyMeshes 
                ? brep.displayValue.SelectMany(m => MeshToNativePretty(m, brep["renderMaterial"] as RenderMaterial)) 
                : brep.displayValue.Select(MeshToNative);
        }
    }
}