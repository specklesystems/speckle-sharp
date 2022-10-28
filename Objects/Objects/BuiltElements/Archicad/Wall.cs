using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad
{
	public sealed class Wall : BuiltElements.Wall
	{
		public int? floorIndex { get; set; }

		public Point startPoint { get; set; }

		public Point endPoint { get; set; }

		public double? arcAngle { get; set; }

		public string structure { get; set; }

		public string geometryMethod { get; set; }

		public string wallComplexity { get; set; }

		public double? thickness { get; set; }

		public double? outsideSlantAngle { get; set; }

		public int? compositeIndex { get; set; }

		public int? buildingMaterialIndex { get; set; }

		public int? profileIndex { get; set; }

		public double baseOffset { get; set; }

		public double? topOffset { get; set; }

		public bool flipped { get; set; }
		public bool hasWindow { get; set; }
		public bool hasDoor { get; set; }

		public Wall() { }

		public Wall(Point startPoint, Point endPoint, double height, bool flipped = false)
		{
			this.startPoint = startPoint;
			this.endPoint = endPoint;
			this.height = height;
			this.flipped = flipped;
		}
	}
}
