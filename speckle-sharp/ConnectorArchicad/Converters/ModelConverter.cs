using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Operations
{
	public static class ModelConverter
	{
		#region --- Functions ---

		public static Base Convert (Model.MeshModel meshDatas)
		{
			Base result = new Base ();
			result["Polygons"] = meshDatas.polygons.Select (p => CreateMeshFromPolygon (meshDatas.vertecies, p)).ToList ();

			return result;
		}

		private static List<double> FlattenPoint (Model.MeshModel.Vertex vertex)
		{
			return new List<double> { vertex.X, vertex.Y, vertex.Z };
		}

		private static List<int> ConvertPolygon (Model.MeshModel.Polygon polygon)
		{
			List<int> vertexIds = new List<int> { polygon.pointIds.Count () == 3 ? 0 : 1 };
			vertexIds.AddRange (Enumerable.Range (0, polygon.pointIds.Count ()));

			return vertexIds;
		}

		private static RenderMaterial ConvertMaterial (Model.MeshModel.Material material)
		{
			System.Drawing.Color ConvertColor (Model.MeshModel.Material.Color color)
			{
				// In AC the Colors are encoded in ushort
				return System.Drawing.Color.FromArgb (color.red / 256, color.green / 256, color.blue / 256);
			}

			return new RenderMaterial
			{
				diffuse = ConvertColor (material.ambientColor).ToArgb (),
				emissive = ConvertColor (material.emissionColor).ToArgb (),
				opacity = 1.0 - material.transparency / 100.0
			};
		}

		private static Mesh CreateMeshFromPolygon (IList<Model.MeshModel.Vertex> vertices, Model.MeshModel.Polygon polygon)
		{
			List<double> points = polygon.pointIds.SelectMany (id => FlattenPoint (vertices[id])).ToList ();
			List<int> polygons = ConvertPolygon (polygon);

			Mesh mesh = new Mesh (points, polygons);
			mesh["renderMaterial"] = ConvertMaterial (polygon.material);

			return mesh;
		}

		#endregion
	}
}