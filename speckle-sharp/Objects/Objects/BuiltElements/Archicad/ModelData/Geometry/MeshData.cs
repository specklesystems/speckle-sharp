using Speckle.Core.Models;
using System.Collections.Generic;


namespace Objects.BuiltElements.Archicad.Model
{
	public sealed class MeshData : Base
	{
		#region --- Classes ---

		public sealed class Polygon : Base
		{
			public IEnumerable<int> pointIds { get; set; }
		}

		#endregion


		#region --- Fields ---

		public IEnumerable<Polygon> polygons { get; set; }

		public IEnumerable<Point3D> vertecies { get; set; }

		#endregion
	}
}