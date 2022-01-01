using Speckle.Core.Models;


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

		public Wall (Model.WallData wallData, Base meshModel)
		{
			WallData = wallData;
			Operations.ModelProvider.Attach (this, meshModel);
		}

		#endregion
	}
}