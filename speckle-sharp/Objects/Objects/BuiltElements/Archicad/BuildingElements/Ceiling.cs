namespace Objects.BuiltElements.Archicad
{
	public sealed class Ceiling : BuiltElements.Ceiling
	{
		#region --- Fields ---

		public Model.CeilingData CeilingData { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public Ceiling ()
        {
        }

		public Ceiling (Model.CeilingData ceilingData, Model.ElementModelData modelData)
		{
			displayMesh = Operations.ModelConverter.Convert (modelData.model);
			CeilingData = ceilingData;
		}

		#endregion
	}
}