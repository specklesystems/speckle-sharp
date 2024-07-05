using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ModelResource))]
public class ModelResourceTests
{
  private Client _testUser;
  private ModelResource Sut => _testUser.Model;
  private Project _project;
  private Model _model;

  [SetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _project = await _testUser.Project.Create(new("Test project", "", null));
    _model = await _testUser.Model.Create(new("Test Model", "", _project.id));
  }

  [TestCase("My Model", "My model description")]
  [TestCase("my/nested/model", null)]
  public async Task ModelCreate(string name, string description)
  {
    CreateModelInput input = new(name, description, _project.id);
    Model result = await Sut.Create(input);

    Assert.That(result, Is.Not.Null);
    Assert.That(result, Has.Property(nameof(result.id)).Not.Null);
    Assert.That(result, Has.Property(nameof(result.name)).EqualTo(input.name).IgnoreCase);
    Assert.That(result, Has.Property(nameof(result.description)).EqualTo(input.description));
  }

  [Test]
  public async Task ModelGet()
  {
    Model result = await Sut.Get(_model.id, _project.id);

    Assert.That(result.id, Is.EqualTo(_model.id));
    Assert.That(result.name, Is.EqualTo(_model.name));
    Assert.That(result.description, Is.EqualTo(_model.description));
    Assert.That(result.createdAt, Is.EqualTo(_model.createdAt));
    Assert.That(result.updatedAt, Is.EqualTo(_model.updatedAt));
  }

  [Test]
  public async Task GetModels()
  {
    var result = await Sut.GetModels(_project.id);

    Assert.That(result.items, Has.Count.EqualTo(1));
    Assert.That(result.totalCount, Is.EqualTo(1));
    Assert.That(result.items[0], Has.Property(nameof(Model.id)).EqualTo(_model.id));
  }

  [Test]
  public async Task Project_GetModels()
  {
    var result = await _testUser.Project.GetWithModels(_project.id);

    Assert.That(result, Has.Property(nameof(Project.id)).EqualTo(_project.id));
    Assert.That(result.models.items, Has.Count.EqualTo(1));
    Assert.That(result.models.totalCount, Is.EqualTo(1));
    Assert.That(result.models.items[0], Has.Property(nameof(Model.id)).EqualTo(_model.id));
  }

  [Test]
  public async Task ModelUpdate()
  {
    const string NEW_NAME = "MY new name";
    const string NEW_DESCRIPTION = "MY new desc";

    UpdateModelInput input = new(_model.id, NEW_NAME, NEW_DESCRIPTION, _project.id);
    Model updatedModel = await Sut.Update(input);

    Assert.That(updatedModel.id, Is.EqualTo(_model.id));
    Assert.That(updatedModel.name, Is.EqualTo(NEW_NAME).IgnoreCase);
    Assert.That(updatedModel.description, Is.EqualTo(NEW_DESCRIPTION));
    Assert.That(updatedModel.updatedAt, Is.GreaterThanOrEqualTo(_model.updatedAt));
  }

  [Test]
  public async Task ModelDelete()
  {
    DeleteModelInput input = new(_model.id, _project.id);

    bool response = await Sut.Delete(input);
    Assert.That(response, Is.True);

    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Get(_model.id, _project.id));
    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Delete(input));
  }
}
