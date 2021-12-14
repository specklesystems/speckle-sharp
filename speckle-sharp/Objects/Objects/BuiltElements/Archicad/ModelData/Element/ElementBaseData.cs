using Speckle.Core.Models;


namespace Objects.BuiltElements.Archicad.Model
{
	public abstract class ElementBaseData : Base
	{
		#region --- Fields ---

		public string elementId { get; set; } = string.Empty;

		public int? floorIndex { get; set; }

		#endregion
	}
}