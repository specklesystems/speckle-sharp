using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
	public sealed class Wall : BuiltElements.Wall
	{

		public Model.WallData WallData { get; set; }
		public List<Mesh> displayValue { get; set; }

		public Wall() { }

		public Wall(Model.WallData wallData, List<Mesh> displayValue)
		{
			WallData = wallData;
			this.displayValue = displayValue;
		}
	}
}
