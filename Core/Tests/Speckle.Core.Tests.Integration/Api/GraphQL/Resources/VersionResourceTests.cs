using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;
using Speckle.Core.Transports;
using Speckle.Core.Transports.ServerUtils;
using Version = Speckle.Core.Api.GraphQL.Models.Version;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(VersionResource))]
public class VersionResourceTests
{
  private Client _testUser;
  private VersionResource Sut => _testUser.Version;
  private Project _project;
  private Model _model1;
  private Model _model2;
  private Version _version;
  private readonly string _objectId = "testobjectid";

  private CommitCreateInput testData =>
    new()
    {
      branchName = "Test Model 1",
      message = "test version",
      objectId = null,
      streamId = _project.id
    };

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _project = await _testUser.Project.Create(new("Test project", "", null));
    _model1 = await _testUser.Model.Create(new(testData.branchName, "", _project.id));
    _model2 = await _testUser.Model.Create(new("Test Model 2", "", _project.id));

    using var remote = new ServerApi(_testUser.Account.serverInfo.url, _testUser.Account.token, "");
    await remote.UploadObjects(_project.id, new[] { (_objectId, "{\"asdf\": \"asdf\"}") });

    string commitId = await _testUser.Version.Create(testData);
    _version = await Sut.Get(_project.id, testData.branchName, commitId);
  }

  [Test]
  public async Task VersionUpdate()
  {
    const string NEW_MESSAGE = "MY new version message";

    UpdateVersionInput input = new(_version.id, NEW_MESSAGE);
    Version updatedVersion = await Sut.Update(input);

    Assert.That(updatedVersion, Has.Property(nameof(Version.id)).EqualTo(_version.id));
    Assert.That(updatedVersion, Has.Property(nameof(Version.message)).EqualTo(NEW_MESSAGE));
    Assert.That(updatedVersion, Has.Property(nameof(Version.previewUrl)).Not.EqualTo(_version.previewUrl));
  }

  [Test]
  public async Task VersionMoveToModel()
  {
    MoveVersionsInput input = new(_model2.name, new[] { _version.id });
    string id = await Sut.MoveToModel(input);
    Version movedVersion = await Sut.Get(id, _model2.id, _project.id);

    Assert.That(movedVersion, Has.Property(nameof(Version.id)).EqualTo(_version.id));
    Assert.That(movedVersion, Has.Property(nameof(Version.message)).EqualTo(_version.message));
    Assert.That(movedVersion, Has.Property(nameof(Version.previewUrl)).Not.EqualTo(_version.previewUrl));

    Assert.ThrowsAsync<SpeckleGraphQLException>(async () => await Sut.Get(id, _model1.id, _project.id));
  }

  [Test]
  public async Task VersionDelete()
  {
    string toDelete = await Sut.Create(testData);
    DeleteVersionsInput input = new(new[] { toDelete });

    bool response = await Sut.Delete(input);
    Assert.That(response, Is.True);

    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Get(toDelete, _model1.id, _project.id));
    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Delete(input));
  }
}
