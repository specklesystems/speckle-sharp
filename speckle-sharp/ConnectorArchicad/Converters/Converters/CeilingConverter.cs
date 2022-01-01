using Objects.BuiltElements.Archicad.Model;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Archicad.Converters
{
	public sealed class Ceiling : IConverter
	{
		#region --- Properties ---

		public Type Type => typeof (Objects.BuiltElements.Archicad.Ceiling);

		#endregion


		#region --- Functions ---

		public async Task<List<string>> ConvertToArchicad (IEnumerable<Base> elements, CancellationToken token)
		{
			IEnumerable<Objects.BuiltElements.Archicad.Ceiling> ceilings = elements.OfType<Objects.BuiltElements.Archicad.Ceiling> ();
			IEnumerable<string> result = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.CreateCeiling (ceilings.Select (x => x.CeilingData)), token);

			return result is null ? new List<string> () : result.ToList ();
		}

		public async Task<List<Base>> ConvertToSpeckle (IEnumerable<Model.ElementModelData> elements, CancellationToken token)
		{
			IEnumerable<CeilingData> datas = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetCeilingData (elements.Select (e => e.elementId)), token);
			if (datas is null)
			{
				return new List<Base> ();
			}

			List<Base> ceilings = new List<Base> ();
			foreach (CeilingData ceilingData in datas)
			{
				Base meshModel = Operations.ModelConverter.Convert (elements.First (e => e.elementId == ceilingData.elementId).model);
				ceilings.Add (new Objects.BuiltElements.Archicad.Ceiling (ceilingData, meshModel));
			}

			return ceilings;
		}

		#endregion
	}
}