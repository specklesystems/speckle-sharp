using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad.Model;

namespace Archicad.Communication.Commands
{
	sealed internal class CreateCeiling : ICommand<IEnumerable<string>>
	{
		#region --- Classes ---

		[JsonObject(MemberSerialization.OptIn)]
		public sealed class Parameters
		{
			#region --- Fields ---

			[JsonProperty("slabs")]
			private IEnumerable<CeilingData> Datas { get; }

			#endregion


			#region --- Ctor \ Dtor ---

			public Parameters(IEnumerable<CeilingData> datas)
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

		private IEnumerable<CeilingData> Datas { get; }

		#endregion


		#region --- Ctor \ Dtor ---

		public CreateCeiling(IEnumerable<CeilingData> datas)
		{
			Datas = datas;
		}

		#endregion


		#region --- Functions ---

		public async Task<IEnumerable<string>> Execute()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("CreateSlab", new Parameters (Datas));
			return result.ElementIds;
		}

		#endregion
	}
}
