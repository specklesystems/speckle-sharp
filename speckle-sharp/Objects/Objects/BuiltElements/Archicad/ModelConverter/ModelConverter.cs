using Objects.Geometry;
using System.Collections.Generic;
using System.Linq;


namespace Objects.BuiltElements.Archicad.Operations
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

				mesh.vertices.AddRange (meshData.vertecies.SelectMany (v => FlattenPoint (v)));
				mesh.faces.AddRange (meshData.polygons.SelectMany (p => ConvertPolygon (p, vertexOffset)));
			}

			return mesh;
		}

		private static List<double> FlattenPoint (Model.Point3D vertex)
		{
			return new List<double> { vertex.x, vertex.y, vertex.z };
		}

		private static List<int> ConvertPolygon (Model.MeshData.Polygon polygon, int vertexOffset)
		{
			List<int> vertexIds = new List<int> ();
			vertexIds.Add (polygon.pointIds.Count () == 3 ? 0 : 1);
			
			foreach (int vertexId in polygon.pointIds)
			{
				vertexIds.Add (vertexId + vertexOffset);
			}

			return vertexIds;
		}

		#endregion
	}
}