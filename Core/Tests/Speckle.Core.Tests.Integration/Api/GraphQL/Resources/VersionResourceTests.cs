using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;
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

  [SetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _project = await _testUser.Project.Create(new("Test project", "", null));
    _model1 = await _testUser.Model.Create(new("Test Model 1", "", _project.id));
    _model2 = await _testUser.Model.Create(new("Test Model 2", "", _project.id));

    string versionId = await Fixtures.CreateVersion(_testUser, _project.id, "Test Model 1");

    _version = await Sut.Get(versionId, _model1.id, _project.id);
  }

  [Test]
  public async Task VersionGet()
  {
    Version result = await Sut.Get(_version.id, _model1.id, _project.id);

    Assert.That(result, Has.Property(nameof(Version.id)).EqualTo(_version.id));
    Assert.That(result, Has.Property(nameof(Version.message)).EqualTo(_version.message));
  }

  [Test]
  public async Task VersionsGet()
  {
    ResourceCollection<Version> result = await Sut.GetVersions(_model1.id, _project.id);

    Assert.That(result.items, Has.Count.EqualTo(1));
    Assert.That(result.totalCount, Is.EqualTo(1));
    Assert.That(result.items[0], Has.Property(nameof(Version.id)).EqualTo(_version.id));
  }

  [Test]
  public async Task VersionReceived()
  {
    CommitReceivedInput input =
      new()
      {
        commitId = _version.id,
        message = "we receieved it",
        sourceApplication = "Integration test",
        streamId = _project.id
      };
    var result = await Sut.Received(input);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task ModelGetWithVersions()
  {
    Model result = await _testUser.Model.GetWithVersions(_model1.id, _project.id);

    Assert.That(result, Has.Property(nameof(Model.id)).EqualTo(_model1.id));
    Assert.That(result.versions.items, Has.Count.EqualTo(1));
    Assert.That(result.versions.totalCount, Is.EqualTo(1));
    Assert.That(result.versions.items[0], Has.Property(nameof(Version.id)).EqualTo(_version.id));
  }

  [Test]
  public async Task VersionUpdate()
  {
    const string NEW_MESSAGE = "MY new version message";

    UpdateVersionInput input = new(_version.id, _project.id, NEW_MESSAGE);
    Version updatedVersion = await Sut.Update(input);

    Assert.That(updatedVersion, Has.Property(nameof(Version.id)).EqualTo(_version.id));
    Assert.That(updatedVersion, Has.Property(nameof(Version.message)).EqualTo(NEW_MESSAGE));
    Assert.That(updatedVersion, Has.Property(nameof(Version.previewUrl)).EqualTo(_version.previewUrl));
  }

  [Test]
  public async Task VersionMoveToModel()
  {
    MoveVersionsInput input = new(_project.id, _model2.name, new[] { _version.id });
    string id = await Sut.MoveToModel(input);
    Assert.That(id, Is.EqualTo(_model2.id));
    Version movedVersion = await Sut.Get(_version.id, _model2.id, _project.id);

    Assert.That(movedVersion, Has.Property(nameof(Version.id)).EqualTo(_version.id));
    Assert.That(movedVersion, Has.Property(nameof(Version.message)).EqualTo(_version.message));
    Assert.That(movedVersion, Has.Property(nameof(Version.previewUrl)).EqualTo(_version.previewUrl));

    Assert.CatchAsync<SpeckleGraphQLException>(async () => await Sut.Get(id, _model1.id, _project.id));
  }

  [Test]
  public async Task VersionDelete()
  {
    DeleteVersionsInput input = new(new[] { _version.id }, _project.id);

    bool response = await Sut.Delete(input);
    Assert.That(response, Is.True);

    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Get(_version.id, _model1.id, _project.id));
    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Delete(input));
  }
}
