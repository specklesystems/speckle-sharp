using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
	public class RevitTopRail : Base, IDisplayValue<List<Mesh>>
	{
		//public string family { get; set; }
		public string type { get; set; }
		public string elementId { get; set; }
		public Base parameters { get; set; }

		[DetachProperty]
		public List<Mesh> displayValue { get; set; }

		public string units { get; set; }

		public RevitTopRail() { }

		[SchemaInfo("TopRail", "Creates a Revit top rail by base curve.", "Revit", "Architecture")]
		public RevitTopRail(string type)
		{
			this.type = type;
		}
	}
}
