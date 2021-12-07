using SpeckleWall = Objects.BuiltElements.Wall;


namespace Archicad.Objects
{
	public sealed class Wall : SpeckleWall
	{
		#region --- Fields ---

		public Model.WallData WallData { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public Wall (Model.WallData wallData, Model.ElementModelData modelData)
		{
			displayMesh = Operations.ModelConverter.Convert (modelData.Model);
			WallData = wallData;
		}

		#endregion
	}
}