using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;

namespace Build;

public static class Github
{
  private static GitHubClient GetClient(string secret) =>
    new(new ProductHeaderValue("Speckle.build"), new InMemoryCredentialStore(new Credentials(secret)));

  public static async Task BuildInstallers(string secret, string runId)
  {
    var client = GetClient(secret);
    await client.Actions.Workflows
      .CreateDispatch(
        "specklesystems",
        "connector-installers",
        "build_installers.yml",
        new CreateWorkflowDispatch("main") { Inputs = new Dictionary<string, object>() { { "run_id", runId } } }
      )
      .ConfigureAwait(false);
  }
}
