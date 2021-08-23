using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConnectorGSATests
{
  public class SendTests
  {
    [Fact]
    public void SendTest()
    {
      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      var converter = new ConverterGSA.ConverterGSA();
      var objs = new List<SpeckleObject>() { new SpeckleObject { applicationId = "test_210820_01" }, new SpeckleObject { applicationId = "test_210820_02" } };
      var errors = new List<string>();

      var streamId = client.StreamCreate(new StreamCreateInput()
      {
        name = "Test-210820-01",
        description = "Testing",
        isPublic = true
      }).Result;

      var commit = new Base();
      var bucket = "Test objects";
      commit[$"{bucket}"] = objs;

      var stream = client.StreamGet(streamId).Result;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var commitObjId = Operations.Send(
        @object: commit,
        transports: transports,
        onErrorAction: (s, e) =>
        {
          errors.Add(e.Message);
        },
        disposeTransports: true
        ).Result;

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = commitObjId,
        branchName = "TestBranch",
        message = "Pushed it real good",
        sourceApplication = Applications.GSA
      };

      //if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = client.CommitCreate(actualCommit).Result;
      }
      catch (Exception e)
      {
        errors.Add(e.Message);
      }
    }
  }
}
