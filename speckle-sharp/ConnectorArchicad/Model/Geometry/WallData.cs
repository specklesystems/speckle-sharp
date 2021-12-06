using Newtonsoft.Json;
using Speckle.Core.Models;


namespace Archicad.Model
{
	[JsonObject(MemberSerialization.OptIn)]
	public sealed class Wall : Base
	{
		[JsonProperty("elementId")]
		public string ElementId { get; private set; }

		public Objects.DirectShape Visualization { get; set; }

		[JsonProperty("floorIndex")]
		public int FloorIndex { get; private set; }

		[JsonProperty("startPoint")]
		public Point3D StartPoint { get; private set; }

		[JsonProperty("endPoint")]
		public Point3D EndPoint { get; private set; }

		[JsonProperty("arcAngle")]
		public double ArcAngle { get; private set; }

		[JsonProperty("height")]
		public double Height { get; private set; }

		[JsonProperty("structure")]
		public string Structure { get; private set; }

		[JsonProperty("geometryMethod")]
		public string GeometryMethod { get; private set; }

		[JsonProperty("wallComplexity")]
		public string WallComplexity { get; private set; }

		[JsonProperty("thickness")]
		public double Thickness { get; private set; }

		[JsonProperty("outsideSlantAngle")]
		public double OutsideSlantAngle { get; private set; }

	}
}