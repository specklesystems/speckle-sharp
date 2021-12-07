using Objects;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Objects
{
	public class DirectShape : Base, IDisplayMesh
	{
		#region --- Fields ---

		public Mesh displayMesh { get; set; }

		public RenderMaterial renderMaterial { get; set; }

		public string ElementId { get; set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public DirectShape ()
		{
		}

		public DirectShape (Model.ElementModelData elementModel)
		{
			ElementId = elementModel.ElementId;
			displayMesh = Operations.ModelConverter.Convert (elementModel.Model);
			renderMaterial = Operations.MaterialConverter.Convert (elementModel.Material);
		}

		#endregion
	}
}