using Objects.Geometry;

namespace Objects.BuiltElements.Archicad.Model
{
	public sealed class WallData : ElementBaseData
	{
		#region --- Fields ---

		public Point startPoint { get; set; }

		public Point endPoint { get; set; }

		public double arcAngle { get; set; }

		public double height { get; set; }

		public string structure { get; set; }

		public string geometryMethod { get; set; }

		public string wallComplexity { get; set; }

		public double thickness { get; set; }

		public double outsideSlantAngle { get; set; }

		#endregion
	}
}