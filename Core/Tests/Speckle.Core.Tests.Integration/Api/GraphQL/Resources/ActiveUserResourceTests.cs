using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ActiveUserResource))]
public class ActiveUserResourceTests
{
  private Client _testUser;
  private ActiveUserResource Sut => _testUser.ActiveUser;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
  }

  [Test]
  public async Task ActiveUserGet()
  {
    var res = await Sut.Get();
    Assert.That(res.id, Is.EqualTo(_testUser.Account.userInfo.id));
  }

  [Test]
  public async Task ActiveUserGet_NonAuthed()
  {
    var result = await Fixtures.Unauthed.ActiveUser.Get();
    Assert.That(result, Is.EqualTo(null));
  }

  [Test]
  public async Task ActiveUserGetProjects()
  {
    var p1 = await _testUser.Project.Create(new("Project 1", null, null));
    var p2 = await _testUser.Project.Create(new("Project 2", null, null));

    var res = await Sut.GetProjects();

    Assert.That(res.items, Has.Exactly(1).Items.With.Property(nameof(Project.id)).EqualTo(p1.id));
    Assert.That(res.items, Has.Exactly(1).Items.With.Property(nameof(Project.id)).EqualTo(p2.id));
    Assert.That(res.items, Has.Count.EqualTo(2));
  }

  [Test]
  public void ActiveUserGetProjects_NoAuth()
  {
    Assert.ThrowsAsync<SpeckleGraphQLException>(async () => await Fixtures.Unauthed.ActiveUser.GetProjects());
  }
}
