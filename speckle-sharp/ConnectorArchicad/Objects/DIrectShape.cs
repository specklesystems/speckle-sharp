using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Objects
{
	public class DirectShape : Base
	{
		#region --- Fields ---

		public IEnumerable<GenericMesh> Meshes { get; private set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public DirectShape ()
		{
		}

		public DirectShape (Model.ElementModel elementModel)
		{
			Meshes = elementModel.Model.Select (mesh => new GenericMesh (mesh));
		}

		#endregion
	}
}