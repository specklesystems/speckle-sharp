using System.Collections.Generic;
using Objects.BuiltElements.Archicad.Model;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
	public sealed class Wall : BuiltElements.Wall
	{
		public string elementId { get; set; } = string.Empty;

		public int? floorIndex { get; set; }

		public List<Mesh> displayValue { get; set; }

		public Point startPoint { get; set; }

		public Point endPoint { get; set; }

		public double? arcAngle { get; set; }

		public double? height { get; set; }

		public string structure { get; set; }

		public string geometryMethod { get; set; }

		public string wallComplexity { get; set; }

		public double? thickness { get; set; }

		public double? outsideSlantAngle { get; set; }

		public int? compositeIndex { get; set; }

		public int? buildingMaterialIndex { get; set; }

		public int? profileIndex { get; set; }

		public Wall() { }
	}
}
