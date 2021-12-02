using Newtonsoft.Json;
using Speckle.Core.Models;
using System.Collections.Generic;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class MeshData : Base
	{
		#region --- Classes ---

		[JsonObject (MemberSerialization.OptIn)]
		public sealed class Vertex : Base
		{
			[JsonProperty ("x")]
			public double X { get; private set; }

			[JsonProperty ("y")]
			public double Y { get; private set; }

			[JsonProperty ("z")]
			public double Z { get; private set; }
		}


		[JsonObject (MemberSerialization.OptIn)]
		public sealed class Polygon : Base
		{
			[JsonProperty ("pointIds")]
			public IEnumerable<int> PointIds { get; private set; }
		}

		#endregion


		#region --- Fields ---

		[JsonProperty ("polygons")]
		public IEnumerable<Polygon> Polygons { get; private set; }

		[JsonProperty ("vertecies")]
		public IEnumerable<Vertex> Vertecies { get; private set; }

		#endregion
	}
}