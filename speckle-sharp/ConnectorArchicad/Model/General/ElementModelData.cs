using Newtonsoft.Json;
using System.Collections.Generic;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class ElementModel
	{
		[JsonProperty ("model")]
		public IEnumerable<MeshData> Model { get; private set; }

		[JsonProperty ("elementId")]
		public string ElementId { get; private set; }
	}
}