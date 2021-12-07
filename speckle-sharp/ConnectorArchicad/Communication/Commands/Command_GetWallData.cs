using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Archicad.Communication.Commands
{
	sealed internal class GetWallData : ICommand<IEnumerable<Model.WallData>>
	{
		#region --- Classes ---

		[JsonObject(MemberSerialization.OptIn)]
		public sealed class Parameters
		{
			#region --- Fields ---

			[JsonProperty("elementIds")]
			private IEnumerable<string> ElementIds { get; }

			#endregion


			#region --- Ctor \ Dtor ---

			public Parameters(IEnumerable<string> elementIds)
			{
				ElementIds = elementIds;
			}

			#endregion
		}


		[JsonObject(MemberSerialization.OptIn)]
		private sealed class Result
		{
			#region --- Fields ---

			[JsonProperty("walls")]
			public IEnumerable<Model.WallData> Datas { get; private set; }

			#endregion
		}

		#endregion


		#region --- Fields ---

		private IEnumerable<string> ElementIds { get; }

		#endregion


		#region --- Ctor \ Dtor ---

		public GetWallData(IEnumerable<string> elementIds)
		{
			ElementIds = elementIds;
		}

		#endregion


		#region --- Functions ---

		public async Task<IEnumerable<Model.WallData>> Execute()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("GetWallData", new Parameters (ElementIds));
			return result.Datas;
		}

		#endregion
	}
}
