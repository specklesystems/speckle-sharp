using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad.Model;


namespace Archicad.Communication.Commands
{
	sealed internal class CreateWall : ICommand<IEnumerable<string>>
	{
		#region --- Classes ---

		[JsonObject(MemberSerialization.OptIn)]
		public sealed class Parameters
		{
			#region --- Fields ---

			[JsonProperty("walls")]
			private IEnumerable<WallData> Datas { get; }

			#endregion


			#region --- Ctor \ Dtor ---

			public Parameters(IEnumerable<WallData> datas)
			{
				Datas = datas;
			}

			#endregion
		}


		[JsonObject(MemberSerialization.OptIn)]
		private sealed class Result
		{
			#region --- Fields ---

			[JsonProperty("elementIds")]
			public IEnumerable<string> ElementIds { get; private set; }

			#endregion
		}

		#endregion


		#region --- Fields ---

		private IEnumerable<WallData> Datas { get; }

		#endregion


		#region --- Ctor \ Dtor ---

		public CreateWall(IEnumerable<WallData> datas)
		{
			Datas = datas;
		}

		#endregion


		#region --- Functions ---

		public async Task<IEnumerable<string>> Execute()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("CreateWall", new Parameters (Datas));
			return result.ElementIds;
		}

		#endregion
	}
}
