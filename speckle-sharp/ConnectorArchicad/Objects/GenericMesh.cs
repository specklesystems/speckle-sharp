using Objects;
using Objects.Geometry;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Objects
{
	public class GenericMesh : Base, IDisplayMesh
	{
		#region --- Fields ---

		public Mesh displayMesh { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public GenericMesh ()
		{
		}

		public GenericMesh (Model.MeshData meshData)
		{
			displayMesh = CreateMesh (meshData);
		}

		#endregion


		#region --- Functions ---

		private List<double> FlattenPoint (Model.MeshData.Vertex vertex)
		{
			return new List<double> { vertex.X, vertex.Y, vertex.Z };
		}

		private List<int> ConvertPolygon (Model.MeshData.Polygon polygon)
		{
			List<int> pointIds = new List<int> ();
			pointIds.Add (polygon.PointIds.Count () == 3 ? 0 : 1);
			pointIds.AddRange (polygon.PointIds);

			return pointIds;
		}

		private Mesh CreateMesh (Model.MeshData meshData)
		{
			List<double> vertecies = meshData.Vertecies.SelectMany (v => FlattenPoint (v)).ToList ();
			List<int> polygons = meshData.Polygons.SelectMany (p => ConvertPolygon (p)).ToList ();

			return new Mesh (vertecies, polygons);
		}

		#endregion
	}
}