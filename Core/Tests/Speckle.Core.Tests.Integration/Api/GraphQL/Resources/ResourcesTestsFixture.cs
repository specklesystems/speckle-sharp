using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[SetUpFixture]
public sealed class ResourcesTestsFixture : IDisposable
{
  public static Client FirstUser { get; private set; }
  public static Client SecondUser { get; private set; }
  public static Client UnauthedUser { get; private set; }
  public static Project Project { get; private set; }

  [OneTimeSetUp]
  public async Task OneTimeSetupCallback() => await OneTimeSetup();

  [OneTimeTearDown]
  public void OneTimeTearDown() => Dispose();

  public async Task OneTimeSetup()
  {
    Dispose();

    var firstUserAccount = await Fixtures.SeedUser();
    SecondUser = new Client(firstUserAccount);
    var secondUserAccount = await Fixtures.SeedUser();
    FirstUser = new Client(secondUserAccount);
    UnauthedUser = new Client(new Account { serverInfo = Fixtures.Server, userInfo = new UserInfo() });

    ProjectCreateInput input =
      new("The best project", "The best description for the best project", ProjectVisibility.Private);
    Project = await FirstUser.Project.Create(input);
  }

  public void Dispose()
  {
    FirstUser?.Dispose();
    SecondUser?.Dispose();
    UnauthedUser?.Dispose();
  }
}
