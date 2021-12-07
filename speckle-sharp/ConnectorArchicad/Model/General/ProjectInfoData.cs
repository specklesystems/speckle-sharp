using Newtonsoft.Json;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	internal sealed class ProjectInfoData
	{
		[JsonProperty ("name")]
		public string Name { get; private set; } = string.Empty;

		[JsonProperty ("location")]
		public string Location { get; private set; } = string.Empty;
	}
}