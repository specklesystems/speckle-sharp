using System.Collections.Generic;


namespace Objects.BuiltElements.Archicad.Model
{
	public sealed class ElementModelData
	{
		#region --- Fields ---

		public IEnumerable<MeshData> model { get; set; } = new List<MeshData> ();

		public string elementId { get; set; } = string.Empty;

		#endregion
	}
}