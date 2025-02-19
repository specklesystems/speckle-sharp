using GraphQL.Client.Http;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.Credentials;

public class UserServerInfoTests
{
  private Account acc;

  [OneTimeSetUp]
  public async Task Setup()
  {
    acc = await Fixtures.SeedUser();
  }

  [Test]
  public async Task IsFrontEnd2True()
  {
    ServerInfo result = await AccountManager.GetServerInfo("https://app.speckle.systems/");

    Assert.That(result, Is.Not.Null);
    Assert.That(result.frontend2, Is.True);
  }

  [Test]
  public void GetServerInfo_ExpectFail_NoServer()
  {
    Uri serverUrl = new("http://invalidserver.local");

    Assert.ThrowsAsync<HttpRequestException>(async () => await AccountManager.GetServerInfo(serverUrl));
  }

  [Test]
  public async Task GetUserInfo()
  {
    Uri serverUrl = new(acc.serverInfo.url);
    UserInfo result = await AccountManager.GetUserInfo(acc.token, serverUrl);

    Assert.That(result.id, Is.EqualTo(acc.userInfo.id));
    Assert.That(result.name, Is.EqualTo(acc.userInfo.name));
    Assert.That(result.email, Is.EqualTo(acc.userInfo.email));
    Assert.That(result.company, Is.EqualTo(acc.userInfo.company));
    Assert.That(result.avatar, Is.EqualTo(acc.userInfo.avatar));
  }

  [Test]
  public void GetUserInfo_ExpectFail_NoServer()
  {
    Uri serverUrl = new("http://invalidserver.local");

    Assert.ThrowsAsync<HttpRequestException>(async () => await AccountManager.GetUserInfo("", serverUrl));
  }

  [Test]
  public void GetUserInfo_ExpectFail_NoUser()
  {
    Uri serverUrl = new(acc.serverInfo.url);

    Assert.ThrowsAsync<GraphQLHttpRequestException>(
      async () => await AccountManager.GetUserInfo("Bearer 08913c3c1e7ac65d779d1e1f11b942a44ad9672ca9", serverUrl)
    );
  }
}
