using System.Collections.Generic;


namespace Archicad.Model
{
	public sealed class MeshModel
	{
		#region --- Classes ---

		public sealed class Vertex
		{
			#region --- Fields ---

			public double X { get; set; }

			public double Y { get; set; }

			public double Z { get; set; }

			#endregion
		}

		public sealed class Material
		{
			#region --- Classes ---

			public class Color
			{
				public int red { get; set; }
				
				public int green { get; set; }
				
				public int blue { get; set; }

			}

			#endregion


			#region --- Fields ---

			public Color ambientColor { get; set; }

			public Color emissionColor { get; set; }

			public double transparency { get; set; }

			#endregion
		}

		public sealed class Polygon
		{
			public List<int> pointIds { get; set; }

			public Material material { get; set; }
		}

		#endregion


		#region --- Fields ---

		public List<Polygon> polygons { get; set; }

		public List<Vertex> vertecies { get; set; }

		#endregion
	}
}