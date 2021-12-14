using Speckle.Core.Models;
using System.Collections.Generic;


namespace Objects.BuiltElements.Archicad.Model
{
	public sealed class ElementShape : Base
	{
		#region --- Classes ---

		public sealed class PolylineSegment : Base
		{
			public Point3D startPoint { get; set; }

			public Point3D endPoint { get; set; }

			public double arcAngle { get; set; }
		}


		public sealed class Polyline : Base
		{
			public IEnumerable<PolylineSegment> polylineSegments { get; set; }
		}

		#endregion


		#region --- Fields ---

		public Polyline contourPolyline { get; set; }

		public IEnumerable<Polyline> holePolylines { get; set; }

		#endregion
	}
}