using Speckle.Core.Models;


namespace Objects.BuiltElements.Archicad
{
	public class DirectShape : Base
	{
		#region --- Fields ---

		public string ElementId { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public DirectShape ()
		{
		}

		public DirectShape (string elementId, Base meshModel)
		{
			ElementId = elementId;
			Operations.ModelProvider.Attach (this, meshModel);
		}

		#endregion
	}
}