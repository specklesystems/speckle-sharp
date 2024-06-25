using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ModelResourceTests
{
  private ProjectResource Sut => ResourcesTestsFixture.FirstUser.Project;
  private Project _project;
}
