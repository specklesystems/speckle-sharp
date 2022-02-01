using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
	public sealed class Ceiling : BuiltElements.Ceiling
	{
		public Model.CeilingData CeilingData { get; set; }

		public List<Mesh> displayValue { get; set; }

		public Ceiling()
		{ }

		public Ceiling(Model.CeilingData ceilingData, List<Mesh> displayValue)
		{
			CeilingData = ceilingData;
			this.displayValue = displayValue;
		}
	}
}
