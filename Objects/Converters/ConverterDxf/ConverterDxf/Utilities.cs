using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;

namespace Objects.Converters.DxfConverter
{
    public struct MeshTopologyResult
    {
        public List<Point> Vertices;
        public List<int[]> Faces;
        public ConcurrentDictionary<Tuple<int, int>, ConcurrentBag<int>> EdgeFaceConnection;
    }
    
    public static class Utilities
    {
        public static MeshTopologyResult GetMeshEdgeFaces(this Mesh mesh)
        {
            var result = new ConcurrentDictionary<Tuple<int,int>, ConcurrentBag<int>>();
            var faces = mesh.GetFaceIndices().ToList();
            var vertices = mesh.GetPoints();
            var faceIndex = 0;
            foreach (var indices in faces)
            {
                for (var j = 0; j < indices.Length; j++)
                {
                    var iA = indices[j];
                    var iB = indices[(j + 1) % indices.Length];
                    var temp = iA;
                    iA = temp < iB ? iA : iB;
                    iB = temp < iB ? iB : temp;
                    var connectedFaces = result.GetOrAdd(new Tuple<int, int>(iA,iB), new ConcurrentBag<int>());
                    connectedFaces.Add(faceIndex);
                }
                faceIndex++;
            }
            
            return new MeshTopologyResult()
            {
                Vertices =  vertices,
                Faces = faces,
                EdgeFaceConnection = result
            };
        }
        public static IEnumerable<int[]> GetFaceIndices(this Mesh mesh)
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
}