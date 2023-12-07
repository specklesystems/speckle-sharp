using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets a given model from a project.
  /// </summary>
  /// <param name="projectId">Id of the project to get the model from</param>
  /// <param name="modelId">Id of the model</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<Model> ModelGet(string projectId, string modelId, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = """
        query ($projectId: String!, $modelId: String!)
        {
           project(id: $projectId) {
             model(id: $modelId){
              id,
              name,
              description,
            }
          }
        }
        """,
      Variables = new { projectId, modelId, }
    };

    var res = await ExecuteGraphQLRequest<ProjectData>(request, cancellationToken).ConfigureAwait(false);
    return res.project.model;
  }
}

public sealed class ProjectData
{
  public Project project { get; set; }
}

public sealed class Project
{
  public Model model { get; set; }
}

public sealed class Model
{
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }
}
