using Speckle.Core.Models;


namespace Objects.BuiltElements.Archicad.Operations
{
	public static class ModelProvider
	{
		#region --- Functions ---

		public static void Attach (Base source, Base meshModel)
		{
			source["Model"] = meshModel;
		}

		#endregion
	}
}