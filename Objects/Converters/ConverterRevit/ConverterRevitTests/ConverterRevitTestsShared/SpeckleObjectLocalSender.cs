using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConnectorRevit;
using ConverterRevitTests;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace ConverterRevitTestsShared
{
  internal class SpeckleObjectLocalSender : ISpeckleObjectSender
  {
    private ISpeckleConverter converter;
    private SpeckleConversionFixture fixture;
    public SpeckleObjectLocalSender(ISpeckleConverter converter, SpeckleConversionFixture fixture)
    {
      this.converter = converter;
      this.fixture = fixture;
    }

    private Base commitObject { get; set; }
    public IEnumerable<Base> GetConvertedObjects()
    {
      var defaultTraversal = DefaultTraversal.CreateBIMTraverseFunc(converter);
      return defaultTraversal.Traverse(commitObject)
        .Select(tc => tc.current)
        .Where(@base => @base.applicationId != null);
    }
    public async Task<string> CreateCommit(Client client, CommitCreateInput actualCommit, CancellationToken cancellationToken)
    {
      var id = Guid.NewGuid().ToString();
      var commitObj = new Commit()
      {
        id = id,
        referencedObject = actualCommit.objectId
      };
      fixture.Commits.Add(id, commitObj);
      return id;
    }

    public async Task<string> Send(Account account, string streamId, Base commitObject, ProgressViewModel progress)
    {
      this.commitObject = commitObject;
      return await Operations.Send(@object: commitObject, cancellationToken: progress.CancellationToken)
        .ConfigureAwait(true);
    }
  }
}
