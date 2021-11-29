using Newtonsoft.Json;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	internal sealed class ProjectInfo
	{
		[JsonProperty ("name")]
		public string Name { get; private set; }

		[JsonProperty ("location")]
		public string Location { get; private set; }
	}
}