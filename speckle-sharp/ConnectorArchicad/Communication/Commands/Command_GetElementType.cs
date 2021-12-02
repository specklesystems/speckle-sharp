using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Archicad.Communication.Commands
{
	internal sealed class GetElementsType : ICommand<IEnumerable<string>>
	{
		#region --- Classes ---

		[JsonObject (MemberSerialization.OptIn)]
		public sealed class Parameters
		{
			#region --- Fields ---

			[JsonProperty ("elementIds")]
			private IEnumerable<string> ElementIds { get; }

			#endregion


			#region --- Ctor \ Dtor ---

			public Parameters (IEnumerable<string> elementIds)
			{
				ElementIds = elementIds;
			}

			#endregion
		}


		[JsonObject (MemberSerialization.OptIn)]
		private sealed class Result
		{
			#region --- Fields ---

			[JsonProperty ("elementTypes")]
			public IEnumerable<string> ElementTypes { get; private set; }

			#endregion
		}

		#endregion


		#region --- Fields ---

		private IEnumerable<string> ElementIds { get; }

		#endregion


		#region --- Ctor \ Dtor ---

		public GetElementsType (IEnumerable<string> elementIds)
		{
			ElementIds = elementIds;
		}

		#endregion


		#region --- Functions ---

		public async Task<IEnumerable<string>> Execute ()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("GetElementTypes", new Parameters (ElementIds));
			return result.ElementTypes;
		}

		#endregion
	}
}
