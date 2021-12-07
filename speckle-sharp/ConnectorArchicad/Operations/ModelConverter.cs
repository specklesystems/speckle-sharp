using Objects.Geometry;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Operations
{
	public static class ModelConverter
	{
		#region --- Functions ---

		public static Mesh Convert (IEnumerable<Model.MeshData> meshDatas)
		{
			Mesh mesh = new Mesh ();

			foreach (Model.MeshData meshData in meshDatas) 
			{
				int vertexOffset = mesh.VerticesCount;

				mesh.vertices.AddRange (meshData.Vertecies.SelectMany (v => FlattenPoint (v)));
				mesh.faces.AddRange (meshData.Polygons.SelectMany (p => ConvertPolygon (p, vertexOffset)));
			}

			return mesh;
		}

		private static List<double> FlattenPoint (Model.MeshData.Vertex vertex)
		{
			return new List<double> { vertex.X, vertex.Y, vertex.Z };
		}

		private static List<int> ConvertPolygon (Model.MeshData.Polygon polygon, int vertexOffset)
		{
			List<int> vertexIds = new List<int> ();
			vertexIds.Add (polygon.VertexIds.Count () == 3 ? 0 : 1);
			
			foreach (int vertexId in polygon.VertexIds)
			{
				vertexIds.Add (vertexId + vertexOffset);
			}

			return vertexIds;
		}

		#endregion
	}
}