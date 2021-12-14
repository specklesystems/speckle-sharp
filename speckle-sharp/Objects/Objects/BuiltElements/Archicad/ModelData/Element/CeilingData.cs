namespace Objects.BuiltElements.Archicad.Model
{
	public sealed class CeilingData : ElementBaseData
	{
		#region --- Fields ---

		public ElementShape shape { get; set; }

		public string structure { get; set; }

		public double? thickness { get; set; }

		public string edgeAngleType { get; set; }

		public double? edgeAngle { get; set; }

		public string referencePlaneLocation { get; set; }

		#endregion
	}
}