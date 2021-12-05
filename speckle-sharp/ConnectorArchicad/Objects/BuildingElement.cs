using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;


namespace Archicad.Objects
{
	public class BuildingElement<TData> where TData : Model.ACElementBaseData
	{
		#region --- Fields ---

		public TData ElementData { get; private set; }
		
		public DirectShape Visualization { get; private set; }

		#endregion


		#region --- Ctor \ Dtor ---

		public BuildingElement (TData data, DirectShape visualization)
		{
			ElementData = data;
			Visualization = visualization;
		}


		#endregion
	}
}