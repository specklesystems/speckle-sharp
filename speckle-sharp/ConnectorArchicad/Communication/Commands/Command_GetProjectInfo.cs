using Objects.BuiltElements.Archicad.Model;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Archicad.Communication.Commands
{
	internal sealed class GetProjectInfo : ICommand<ProjectInfoData>
	{
		#region --- Classes ---

		[JsonObject]
		public sealed class Parameters
		{
		}

		#endregion


		#region --- Functions ---

		public async Task<ProjectInfoData> Execute ()
		{
			return await HttpCommandExecutor.Execute<Parameters, ProjectInfoData> ("GetProjectInfo", null);
		}

		#endregion
	}
}
