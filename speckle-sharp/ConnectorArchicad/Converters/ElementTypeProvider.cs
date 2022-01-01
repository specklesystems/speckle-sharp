using Objects.BuiltElements.Archicad;
using System;


namespace Archicad
{
	public static class ElementTypeProvider
	{
		#region --- Functions ---

		public static Type GetTypeByName (string name)
		{
			switch (name)
			{
				case "Wall":	return typeof (Wall);
				case "Slab":	return typeof (Ceiling);
				default:		return typeof (DirectShape);
			}
		}

		#endregion
	}
}