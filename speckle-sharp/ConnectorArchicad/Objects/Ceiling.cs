using SpeckleCeiling = Objects.BuiltElements.Ceiling;


namespace Archicad.Objects
{
	public sealed class Ceiling : SpeckleCeiling
	{
		#region --- Fields ---

		public Model.CeilingData CeilingData { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public Ceiling (Model.CeilingData ceilingData, Model.ElementModelData modelData)
		{
			displayMesh = Operations.ModelConverter.Convert (modelData.Model);
			CeilingData = ceilingData;
		}

		#endregion
	}
}