using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Archicad.Model;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateObject : ICommand<IEnumerable<ApplicationObject>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("objects")]
      private IEnumerable<ArchicadObject> Objects { get; }
      [JsonProperty("meshModels")]
      private IEnumerable<MeshModel> MeshModels { get; }


      public Parameters(IEnumerable<ArchicadObject> objects, IEnumerable<MeshModel> meshModels)
      {
        Objects = objects;
        MeshModels = meshModels;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationObjects")]
      public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }
    }

    private IEnumerable<ArchicadObject> Objects { get; }
    private IEnumerable<MeshModel> MeshModels { get; }

    public CreateObject(IEnumerable<ArchicadObject> objects, IEnumerable<MeshModel> meshModels)
    {
      Objects = objects;
      MeshModels = meshModels;
    }

    public async Task<IEnumerable<ApplicationObject>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateObject", new Parameters(Objects, MeshModels));
      return result == null ? null : result.ApplicationObjects;
    }
  }
}
