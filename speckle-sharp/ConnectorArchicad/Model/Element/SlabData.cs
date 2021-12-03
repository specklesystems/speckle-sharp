using Newtonsoft.Json;
using Speckle.Core.Models;
using System.Collections.Generic;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class SlabData : Base
	{
		#region --- Fields ---

		[JsonProperty ("elementId")]
		public string ElementId { get; private set; }

		[JsonProperty ("floorIndex")]
		public int? FloorIndex { get; private set; }

		[JsonProperty ("shape")]
		public ElementShape Shape { get; private set; }

		[JsonProperty ("structure")]
		public string Structure { get; private set; }

		[JsonProperty ("thickness")]
		public double? Thickness { get; private set; }

		[JsonProperty ("edgeAngleType")]
		public string EdgeAngleType { get; private set; }

		[JsonProperty ("edgeAngle")]
		public double? EdgeAngle { get; private set; }

		[JsonProperty ("referencePlaneLocation")]
		public string ReferencePlaneLocation { get; private set; }

		#endregion
	}
}