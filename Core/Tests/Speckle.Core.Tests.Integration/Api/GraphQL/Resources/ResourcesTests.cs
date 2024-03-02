using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

public abstract class ResourcesTests : IDisposable
{
  protected Client FirstUser { get; private set; }
  protected Client SecondUser { get; private set; }

  [OneTimeSetUp]
  public async Task OneTimeSetupCallback()
  {
    await OneTimeSetup();
  }

  protected virtual async Task OneTimeSetup()
  {
    var firstUserAccount = await Fixtures.SeedUser();
    SecondUser = new Client(firstUserAccount);
    var secondUserAccount = await Fixtures.SeedUser();
    FirstUser = new Client(secondUserAccount);
  }

  public virtual void Dispose()
  {
    FirstUser.Dispose();
    SecondUser.Dispose();
  }
}

public abstract class ResourcesExceptionalTests : ResourcesTests
{
  protected Client Unauthed { get; private set; }

  protected override async Task OneTimeSetup()
  {
    await base.OneTimeSetup();
    Unauthed = new Client(new Account { serverInfo = Fixtures.Server, userInfo = new UserInfo() });
  }

  public override void Dispose()
  {
    base.Dispose();
    Unauthed.Dispose();
  }
}
