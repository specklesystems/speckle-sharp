using GraphQL.Client.Http;
using Speckle.Core.Api;
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
  public async Task GetServerInfo()
  {
    Uri serverUrl = new(acc.serverInfo.url);
    ServerInfo result = await AccountManager.GetServerInfo(serverUrl);

    Assert.That(new Uri(result.url), Is.EqualTo(new Uri(acc.serverInfo.url)));
    Assert.That(result.name, Is.Not.Null);
    Assert.That(result.frontend2, Is.False);
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

  [Test]
  public async Task GetUserServerInfo()
  {
    Uri serverUrl = new(acc.serverInfo.url);
    var result = await AccountManager.GetUserServerInfo(acc.token, serverUrl);

    Assert.That(new Uri(result.serverInfo.url), Is.EqualTo(new Uri(acc.serverInfo.url)));
    Assert.That(result.serverInfo.name, Is.Not.Null);
    Assert.That(result.serverInfo.frontend2, Is.False);

    Assert.That(result.activeUser.id, Is.EqualTo(acc.userInfo.id));
    Assert.That(result.activeUser.name, Is.EqualTo(acc.userInfo.name));
    Assert.That(result.activeUser.email, Is.EqualTo(acc.userInfo.email));
    Assert.That(result.activeUser.company, Is.EqualTo(acc.userInfo.company));
    Assert.That(result.activeUser.avatar, Is.EqualTo(acc.userInfo.avatar));
  }
}
