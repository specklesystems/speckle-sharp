using Objects.Other;
using System.Drawing;


namespace Archicad.Operations
{
	public static class MaterialConverter
	{
		#region --- Functions ---

		public static RenderMaterial Convert (Model.MeshData.Material material)
		{
			if (material is null)
			{
				return new RenderMaterial ();
			}

			return new RenderMaterial { diffuse = ConvertToColorToInt (material.AmbientColor) };
		}


		private static int ConvertToColorToInt (Model.MeshData.Material.Color color)
		{
			byte r = System.Convert.ToByte (((double)color.Red / ushort.MaxValue) * byte.MaxValue);
			byte g = System.Convert.ToByte (((double)color.Green / ushort.MaxValue) * byte.MaxValue);
			byte b = System.Convert.ToByte (((double)color.Blue / ushort.MaxValue) * byte.MaxValue);

			return Color.FromArgb (r, g, b).ToArgb ();

		}

		#endregion
	}
}
