using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;

namespace Build;

public static class Github
{
  private static GitHubClient GetClient(string secret) =>
    new(new Octokit.ProductHeaderValue("Speckle.build"), new InMemoryCredentialStore(new Credentials(secret)));

  public static async Task TriggerWorkflow(string secret, string workflowFileName)
  {
    var client = GetClient(secret);
    await client.Actions.Workflows
      .CreateDispatch("specklesystems", "connector-installers", workflowFileName, new CreateWorkflowDispatch("main"))
      .ConfigureAwait(false);
  }
}
