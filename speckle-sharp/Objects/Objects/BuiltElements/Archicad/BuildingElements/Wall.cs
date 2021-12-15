namespace Objects.BuiltElements.Archicad
{
	public sealed class Wall : BuiltElements.Wall
	{
		#region --- Fields ---

		public Model.WallData WallData { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public Wall ()
		{ 
		}

		public Wall (Model.WallData wallData, Model.ElementModelData modelData)
		{
			displayMesh = Operations.ModelConverter.Convert (modelData.model);
			WallData = wallData;
		}

		#endregion
	}
}