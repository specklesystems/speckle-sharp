using Objects.BuiltElements.Archicad.Model;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Archicad.Converters
{
	public sealed class DirectShape : IConverter
	{
		#region --- Properties ---

		public Type Type => null;

		#endregion


		#region --- Functions ---

		public async Task<List<string>> ConvertToArchicad (IEnumerable<Base> elements, CancellationToken token)
		{
			return new List<string> ();
		}

		public Task<List<Base>> ConvertToSpeckle (IEnumerable<Model.ElementModelData> elements, CancellationToken token)
		{
			return Task.FromResult (new List<Base> (elements.Select (e => new Objects.BuiltElements.Archicad.DirectShape (e.elementId, Operations.ModelConverter.Convert (e.model)))));
		}

		#endregion
	}
}