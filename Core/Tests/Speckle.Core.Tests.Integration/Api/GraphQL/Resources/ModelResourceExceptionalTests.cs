using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ModelResource))]
public class ModelResourceExceptionalTests
{
  private Client _testUser;
  private ModelResource Sut => _testUser.Model;
  private Project _project;
  private Model _model;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _project = await _testUser.Project.Create(new("Test project", "", ProjectVisibility.Private));
    _model = await _testUser.Model.Create(new("Test Model", "", _project.id));
  }

  [TestCase(null)]
  [TestCase("")]
  [TestCase(" ")]
  public void ModelCreate_Throws_InvalidInput(string name)
  {
    CreateModelInput input = new(name, null, _project.id);
    Assert.CatchAsync<SpeckleGraphQLException>(async () => await Sut.Create(input));
  }

  [Test]
  public void ModelGet_Throws_NoAuth()
  {
    Assert.CatchAsync<SpeckleGraphQLException>(async () => await Fixtures.Unauthed.Model.Get(_model.id, _project.id));
  }

  [Test]
  public void ModelGet_Throws_NonExistentModel()
  {
    Assert.CatchAsync<SpeckleGraphQLException>(async () => await Sut.Get("non existent model", _project.id));
  }

  [Test]
  public void ModelGet_Throws_NonExistentProject()
  {
    Assert.ThrowsAsync<SpeckleGraphQLStreamNotFoundException>(
      async () => await Sut.Get(_model.id, "non existent project")
    );
  }

  [Test]
  public void ModelUpdate_Throws_NonExistentModel()
  {
    UpdateModelInput input = new("non-existent model", "MY new name", "MY new desc", _project.id);

    Assert.CatchAsync<SpeckleGraphQLException>(async () => await Sut.Update(input));
  }

  [Test]
  public void ModelUpdate_Throws_NonExistentProject()
  {
    UpdateModelInput input = new(_model.id, "MY new name", "MY new desc", "non-existent project");

    Assert.ThrowsAsync<SpeckleGraphQLForbiddenException>(async () => await Sut.Update(input));
  }

  [Test]
  public void ModelUpdate_Throws_NonAuthProject()
  {
    UpdateModelInput input = new(_model.id, "MY new name", "MY new desc", _project.id);

    Assert.CatchAsync<SpeckleGraphQLException>(async () => await Fixtures.Unauthed.Model.Update(input));
  }

  [Test]
  public async Task ModelDelete_Throws_NoAuth()
  {
    Model toDelete = await Sut.Create(new("Delete me", null, _project.id));
    DeleteModelInput input = new(toDelete.id, _project.id);
    bool response = await Sut.Delete(input);
    Assert.That(response, Is.True);

    Assert.CatchAsync<SpeckleGraphQLException>(async () => _ = await Sut.Delete(input));
  }
}
