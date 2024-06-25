using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectInviteResourceTests
{
  private Client _testUser;
  private Account _targetUser;
  private Project _project;
  private PendingStreamCollaborator _createdInvite;
  private ProjectInviteResource Sut => _testUser.ProjectInvite;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _targetUser = await Fixtures.SeedUser();
    _project = await _testUser.Project.Create(new("test", null, null));
  }

  [Test]
  public async Task ProjectInviteCreate_By_Email()
  {
    ProjectInviteCreateInput input = new(_targetUser.userInfo.email, null, null, null);
    var res = await Sut.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(_project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(_targetUser.userInfo.id));

    _createdInvite = res.invitedTeam[0];
  }

  [Test]
  public async Task ProjectInviteCreate_By_UserId()
  {
    ProjectInviteCreateInput input = new(null, null, null, _targetUser.userInfo.id);
    var res = await Sut.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(_project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(_targetUser.userInfo.id));
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
    PendingStreamCollaborator collaborator = await Sut.Get(_project.id, _createdInvite.token);

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
}
