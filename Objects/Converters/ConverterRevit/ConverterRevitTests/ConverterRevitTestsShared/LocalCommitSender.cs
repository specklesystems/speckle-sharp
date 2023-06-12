using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConnectorRevit;
using ConverterRevitTests;
using DesktopUI2;
using DesktopUI2.ViewModels;
using Objects.Organization;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Transports;

namespace ConverterRevitTestsShared
{
  internal class LocalCommitSender : ISpeckleObjectSender
  {
    private ISpeckleConverter converter;
    private SpeckleConversionFixture fixture;
    public LocalCommitSender(ISpeckleConverter converter, SpeckleConversionFixture fixture)
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
        .Where(tc => converter.CanConvertToNative(tc));
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

    ///// <summary>
    ///// Traverses until finds a convertable object (or fallback) then traverses members
    ///// </summary>
    ///// <param name="converter"></param>
    ///// <returns></returns>
    //private static GraphTraversal CreateBIMTraverseFunc(ISpeckleConverter converter)
    //{
    //  var bimElementRule = TraversalRule
    //    .NewTraversalRule()
    //    .When(converter.CanConvertToNative)
    //    .ContinueTraversing(DefaultTraversal.ElementsAliases);

    //  //WORKAROUND: ideally, traversal rules would not have Objects specific rules.
    //  var ignoreResultsRule = TraversalRule
    //    .NewTraversalRule()
    //    .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
    //    .ContinueTraversing(DefaultTraversal.None);

    //  var ignoreProjectInfoRule

    //  var defaultRule = TraversalRule.NewTraversalRule().When(_ => true).ContinueTraversing(DefaultTraversal.Members());

    //  return new GraphTraversal(bimElementRule, ignoreResultsRule, defaultRule);
    //}
  }
}
