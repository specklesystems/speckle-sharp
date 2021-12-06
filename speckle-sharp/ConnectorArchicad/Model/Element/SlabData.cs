using Newtonsoft.Json;
using Speckle.Core.Models;
using System.Collections.Generic;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class SlabData : ACElementBaseData
	{
		#region --- Fields ---

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