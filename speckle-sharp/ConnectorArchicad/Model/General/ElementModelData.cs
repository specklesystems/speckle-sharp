using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Model
{
	[JsonObject (MemberSerialization.OptIn)]
	public sealed class ElementModelData
	{
		#region --- Fields ---

		[JsonProperty ("model")]
		public IEnumerable<MeshData> Model { get; private set; } = new List<MeshData> ();

		[JsonProperty ("elementId")]
		public string ElementId { get; private set; } = string.Empty;

		#endregion


		#region --- Properties ---

		public MeshData.Material Material
		{
			get
			{
				if (!Model.Any ())
				{
					return null;
				}

				return Model.First ().DisplayMaterial;
			}
		}


		#endregion
	}
}