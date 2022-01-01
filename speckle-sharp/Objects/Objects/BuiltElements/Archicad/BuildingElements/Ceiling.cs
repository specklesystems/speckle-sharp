using Speckle.Core.Models;


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

		public Ceiling (Model.CeilingData ceilingData, Base meshModel)
		{
			CeilingData = ceilingData;
			Operations.ModelProvider.Attach (this, meshModel);
		}

		#endregion
	}
}