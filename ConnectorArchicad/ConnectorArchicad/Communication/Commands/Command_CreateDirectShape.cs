using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands;

sealed internal class CreateDirectShape : ICommand<IEnumerable<ApplicationObject>>
{
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters
  {
    [JsonProperty("directShapes")]
    private IEnumerable<DirectShape> DirectShapes { get; }

    public Parameters(IEnumerable<DirectShape> directShapes)
    {
      DirectShapes = directShapes;
    }
  }

  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("applicationObjects")]
    public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }
  }

  private IEnumerable<DirectShape> DirectShapes { get; }

  public CreateDirectShape(IEnumerable<DirectShape> directShapes)
  {
    foreach (var directShape in directShapes)
    {
      directShape.displayValue = null;
    }

    DirectShapes = directShapes;
  }

  public async Task<IEnumerable<ApplicationObject>> Execute()
  {
    var result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "CreateDirectShape",
      new Parameters(DirectShapes)
    );
    return result == null ? null : result.ApplicationObjects;
  }
}
