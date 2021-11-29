using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Archicad.Communication.Commands
{
	internal sealed class GetProjectInfo : ICommand<Model.ProjectInfo>
	{
		#region --- Classes ---

		[JsonObject]
		public sealed class Parameters
		{
		}

		#endregion


		#region --- Functions ---

		public async Task<Model.ProjectInfo> Execute ()
		{
			return await HttpCommandExecutor.Execute<Parameters, Model.ProjectInfo> ("GetProjectInfo", null);
		}

		#endregion
	}
}
