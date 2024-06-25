using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectInviteResourceTests
{
  private Client _testUser,
    _secondUser;
  private Project _project;
  private PendingStreamCollaborator _createdInvite;
  private ProjectInviteResource Sut => _testUser.ProjectInvite;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _secondUser = await Fixtures.SeedUserWithClient();
    _project = await _testUser.Project.Create(new("test", null, null));
  }

  [Test]
  public async Task ProjectInviteCreate_By_Email()
  {
    ProjectInviteCreateInput input = new(_secondUser.Account.userInfo.email, null, ServerRoles.STREAM_REVIEWER, null);
    var res = await Sut.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(_project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(_secondUser.Account.userInfo.id));
    Assert.That(res.invitedTeam[0].token, Is.Not.Null);

    _createdInvite = res.invitedTeam[0];
  }

  [Test]
  public async Task ProjectInviteCreate_By_UserId()
  {
    ProjectInviteCreateInput input = new(null, null, null, _secondUser.Account.userInfo.id);
    var res = await Sut.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(_project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(_secondUser.Account.userInfo.id));
  }

  [Test]
  public void ProjectInviteCreate_InvalidInput()
  {
    Assert.CatchAsync<SpeckleGraphQLException>(async () =>
    {
      var input = new ProjectInviteCreateInput(null, null, null, null);
      await Sut.Create(_project.id, input);
    });

    Assert.CatchAsync<SpeckleGraphQLException>(async () =>
    {
      var input = new ProjectInviteCreateInput(null, "something", "something", null);
      await Sut.Create(_project.id, input);
    });
  }

  [Test]
  public async Task ProjectInviteGet()
  {
    await ProjectInviteCreate_By_Email();
    var collaborator = await Sut.Get(_project.id, _createdInvite.token);

    Assert.That(collaborator, Has.Property(nameof(PendingStreamCollaborator.id)).EqualTo(_createdInvite.id));
    Assert.That(
      collaborator,
      Has.Property(nameof(PendingStreamCollaborator.inviteId)).EqualTo(_createdInvite.inviteId)
    );
  }

  [Test]
  public async Task ProjectInviteUse()
  {
    await ProjectInviteCreate_By_Email();
    ProjectInviteUseInput input = new(true, _createdInvite.streamId, _createdInvite.token);
    var res = await Sut.Use(input);

    Assert.That(res, Is.True);
  }

  [Test]
  [TestCase("stream:owner")]
  [TestCase("stream:reviewer")] //TODO: be exhaustive
  [TestCase(null)] //Revoke access
  public async Task ProjectUpdateRole(string newRole)
  {
    //TODO: figure out if this test could work, we may need to invite the user first...
    ProjectUpdateRoleInput input = new(_secondUser.Account.userInfo.id, _project.id, newRole);
    _ = await _testUser.Project.UpdateRole(input);

    Project finalProject = await _secondUser.Project.Get(_secondUser.Account.id);
    Assert.That(finalProject.role, Is.EqualTo(newRole));
  }
}
