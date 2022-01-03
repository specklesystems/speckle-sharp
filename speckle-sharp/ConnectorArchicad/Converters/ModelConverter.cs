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

		public static Base Convert (Model.MeshModel meshModel)
		{
			Base result = new Base ();
			result["Polygons"] = meshModel.polygons.Select (p => CreateMeshFromPolygon (meshModel.vertecies, p, meshModel.materials[p.material])).ToList ();

			return result;
		}

		public static Model.MeshModel Convert (Mesh mesh)
		{
			return new Model.MeshModel
			{
				vertecies = mesh.GetPoints ().Select (p => new Model.MeshModel.Vertex { x = p.x, y = p.y, z = p.z }).ToList (),
				polygons = ConvertPolygon (mesh.faces)
			};
		}

		public static Model.MeshModel Convert (IEnumerable<Mesh> meshes)
		{
			Model.MeshModel meshModel = new Model.MeshModel ();

			foreach (Mesh mesh in meshes)
			{
				int vertexOffset = meshModel.vertecies.Count;
				List<Model.MeshModel.Polygon> polygons = ConvertPolygon (mesh.faces);
				polygons.ForEach (p => p.pointIds = p.pointIds.Select (l => l + vertexOffset).ToList ());

				meshModel.vertecies.AddRange (mesh.GetPoints ().Select (p => new Model.MeshModel.Vertex { x = p.x, y = p.y, z = p.z }));
				meshModel.polygons.AddRange (polygons);
			}

			return meshModel;
		}

		private static List<double> FlattenPoint (Model.MeshModel.Vertex vertex)
		{
			return new List<double> { vertex.x, vertex.y, vertex.z };
		}

		private static List<int> ConvertPolygon (Model.MeshModel.Polygon polygon)
		{
			List<int> vertexIds = new List<int> { polygon.pointIds.Count () == 3 ? 0 : 1 };
			vertexIds.AddRange (Enumerable.Range (0, polygon.pointIds.Count ()));

			return vertexIds;
		}

		private static List<Model.MeshModel.Polygon> ConvertPolygon (List<int> polygon)
		{
			List<Model.MeshModel.Polygon> result = new List<Model.MeshModel.Polygon> ();

			for (int i = 0; i < polygon.Count; i++)
			{
				int step = polygon[i] == 0 ? 3 : 4;
				result.Add (new Model.MeshModel.Polygon { pointIds = polygon.GetRange (i + 1, step) });
				i += step;
			}

			return result;
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

		private static Mesh CreateMeshFromPolygon (IList<Model.MeshModel.Vertex> vertices, Model.MeshModel.Polygon polygon, Model.MeshModel.Material material)
		{
			List<double> points = polygon.pointIds.SelectMany (id => FlattenPoint (vertices[id])).ToList ();
			List<int> polygons = ConvertPolygon (polygon);

			Mesh mesh = new Mesh (points, polygons);
			mesh["renderMaterial"] = ConvertMaterial (material);

			return mesh;
		}

		#endregion
	}
}