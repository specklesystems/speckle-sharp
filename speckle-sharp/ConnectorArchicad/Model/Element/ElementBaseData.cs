using Newtonsoft.Json;
using Speckle.Core.Models;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public abstract class ElementBaseData : Base
	{
		#region --- Fields ---

		[JsonProperty ("elementId")]
		public string ElementId { get; private set; } = string.Empty;

		[JsonProperty ("floorIndex")]
		public int? FloorIndex { get; private set; }


		#endregion
	}
}