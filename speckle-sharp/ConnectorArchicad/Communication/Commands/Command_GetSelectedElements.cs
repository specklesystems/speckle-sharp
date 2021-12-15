using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Archicad.Communication.Commands
{
	internal sealed class GetSelectedElements : ICommand<IEnumerable<string>>
	{
		#region --- Classes ---

		[JsonObject]
		public sealed class Parameters
		{
		}


		[JsonObject (MemberSerialization.OptIn)]
		private sealed class Result
		{
			#region --- Fields ---

			[JsonProperty ("selectedElementIds")]
			public IEnumerable<string> ElementIds { get; private set; }

			#endregion
		}

		#endregion


		#region --- Functions ---

		public async Task<IEnumerable<string>> Execute ()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("GetSelectedElementIds", null);
			return result.ElementIds;
		}

		#endregion
	}
}
