using Newtonsoft.Json;
using Speckle.Core.Models;
using System.Collections.Generic;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class ElementShape : Base
	{
		#region --- Classes ---

		[JsonObject (MemberSerialization.OptIn)]
		public sealed class PolylineSegment : Base
		{
			[JsonProperty ("startPoint")]
			public Point3D StartPoint { get; private set; }

			[JsonProperty ("endPoint")]
			public Point3D EndPoint { get; private set; }

			[JsonProperty ("arcAngle")]
			public double ArcAngle { get; private set; }
		}


		[JsonObject (MemberSerialization.OptIn)]
		public sealed class Polyline : Base
		{
			[JsonProperty ("polylineSegments")]
			public IEnumerable<PolylineSegment> PolylineSegments { get; private set; }
		}

		#endregion


		#region --- Fields ---

		[JsonProperty ("contourPolyline")]
		public Polyline Contour { get; private set; }

		[JsonProperty ("holePolylines")]
		public IEnumerable<Polyline> Holes { get; private set; }

		#endregion
	}
}