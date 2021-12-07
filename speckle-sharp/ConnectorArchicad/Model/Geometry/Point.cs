using Newtonsoft.Json;
using Speckle.Core.Models;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class Point3D : Base
	{
		[JsonProperty ("x")]
		public double X { get; private set; }

		[JsonProperty ("y")]
		public double Y { get; private set; }

		[JsonProperty ("z")]
		public double Z { get; private set; }
	}
	

	[JsonObject (MemberSerialization.OptIn)]
	public sealed class Point2D : Base
	{
		[JsonProperty ("x")]
		public double X { get; private set; }

		[JsonProperty ("y")]
		public double Y { get; private set; }
	}
}