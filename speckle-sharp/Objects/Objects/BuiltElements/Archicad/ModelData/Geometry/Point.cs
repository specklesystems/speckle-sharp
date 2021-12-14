using Speckle.Core.Models;


namespace Objects.BuiltElements.Archicad.Model
{
	public sealed class Point3D : Base
	{
		public double x { get; set; }

		public double y { get; set; }

		public double z { get; set; }
	}
	

	public sealed class Point2D : Base
	{
		public double x { get; set; }

		public double y { get; set; }
	}
}