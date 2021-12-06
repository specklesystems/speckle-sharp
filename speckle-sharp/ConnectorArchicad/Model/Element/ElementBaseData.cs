using Newtonsoft.Json;
using Speckle.Core.Models;
using System.Collections.Generic;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public abstract class ACElementBaseData : Base
	{
		#region --- Fields ---

		[JsonProperty ("elementId")]
		public string ElementId { get; private set; }

		[JsonProperty ("floorIndex")]
		public int? FloorIndex { get; private set; }


		#endregion
	}
}