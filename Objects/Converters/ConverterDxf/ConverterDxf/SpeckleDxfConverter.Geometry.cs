using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Dxf = netDxf;
using Dxfe = netDxf.Entities;

namespace ConverterDxf
{
    public partial class SpeckleDxfConverter
    {
        public netDxf.Vector3 VectorToNative(Point pt) => VectorToNative(new Vector(pt));
        public netDxf.Vector3 VectorToNative(Vector pt) => new(pt.x, pt.y, pt.z);
        public netDxf.Entities.Point PointToNative(Point pt) => new(VectorToNative(pt));

        public netDxf.Entities.Line LineToNative(Line line) =>
            new(VectorToNative(line.start), VectorToNative(line.end));

        public Dxfe.Mesh MeshToNative(Mesh mesh) => new(
            mesh.GetPoints().Select(VectorToNative),
            mesh.GetFaceIndices()
        );

        public IEnumerable<Dxfe.EntityObject> MeshToNativePretty(Mesh mesh)
        {
            var topology = mesh.GetMeshEdgeFaces();
            return MeshFacesToNative(topology).Concat<Dxfe.EntityObject>(MeshEdgesToNative(topology));
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

        private IEnumerable<Dxfe.Face3D> MeshFacesToNative(MeshTopologyResult topology, string layerName = "Mesh Faces")
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
                return face;
            }).Where(f => f != null);
        }


        public IEnumerable<Dxfe.EntityObject> BrepToNative(Brep brep)
        {
            return Settings.PrettyMeshes 
                ? brep.displayValue.SelectMany(MeshToNativePretty) 
                : brep.displayValue.Select(MeshToNative);
        }
    }
}