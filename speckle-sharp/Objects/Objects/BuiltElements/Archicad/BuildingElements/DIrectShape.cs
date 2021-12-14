using Objects.Geometry;
using Speckle.Core.Models;


namespace Objects.BuiltElements.Archicad
{
	public class DirectShape : Base, IDisplayMesh
	{
		#region --- Fields ---

		public Mesh displayMesh { get; set; }

		public string ElementId { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public DirectShape ()
		{
		}

		public DirectShape (Model.ElementModelData elementModel)
		{
			ElementId = elementModel.elementId;
			displayMesh = Operations.ModelConverter.Convert (elementModel.model);
		}

		#endregion
	}
}