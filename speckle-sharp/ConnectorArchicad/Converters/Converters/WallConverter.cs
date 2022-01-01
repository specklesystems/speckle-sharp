using Objects.BuiltElements.Archicad.Model;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Archicad.Converters
{
	public sealed class Wall : IConverter
	{
		#region --- Properties ---

		public Type Type => typeof (Objects.BuiltElements.Archicad.Wall);

		#endregion


		#region --- Functions ---

		public async Task<List<string>> ConvertToArchicad (IEnumerable<Base> elements, CancellationToken token)
		{
			IEnumerable<Objects.BuiltElements.Archicad.Wall> walls = elements.OfType<Objects.BuiltElements.Archicad.Wall> ();
			IEnumerable<string> result = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.CreateWall (walls.Select (x => x.WallData)), token);

			return result is null ? new List<string> () : result.ToList ();
		}

		public async Task<List<Base>> ConvertToSpeckle (IEnumerable<Model.ElementModelData> elements, CancellationToken token)
		{
			IEnumerable<WallData> datas = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetWallData (elements.Select (e => e.elementId)), token);
			if (datas is null)
			{
				return new List<Base> ();
			}

			List<Base> walls = new List<Base> ();
			foreach (WallData wallData in datas)
			{
				Base meshModel = Operations.ModelConverter.Convert (elements.First (e => e.elementId == wallData.elementId).model);
				walls.Add (new Objects.BuiltElements.Archicad.Wall (wallData, meshModel));
			}

			return walls;
		}

		#endregion
	}
}