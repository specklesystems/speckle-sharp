using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad.Model;


namespace Archicad.Communication.Commands
{
	internal sealed class GetCeilingData : ICommand<IEnumerable<CeilingData>>
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

			[JsonProperty ("slabs")]
			public IEnumerable<CeilingData> Datas { get; private set; }

			#endregion
		}

		#endregion


		#region --- Fields ---

		private IEnumerable<string> ElementIds { get; }

		#endregion


		#region --- Ctor \ Dtor ---

		public GetCeilingData (IEnumerable<string> elementIds)
		{
			ElementIds = elementIds;
		}

		#endregion


		#region --- Functions ---

		public async Task<IEnumerable<CeilingData>> Execute ()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("GetSlabData", new Parameters (ElementIds));
			return result.Datas;
		}

		#endregion
	}
}
