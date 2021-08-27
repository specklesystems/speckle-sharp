using ConnectorGSA;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Stream = Speckle.Core.Api.Stream;

namespace ConnectorGSATests
{
  public class ReceiveTests : SpeckleConnectorFixture
  {
    [Fact]
    public async void ReceiveTest()
    {
      Instance.GsaModel.Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();
      Instance.GsaModel.Layer = GSALayer.Design;

      var testObj = new SpeckleObject();

      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      var streamState = await LatestStream(client);

      //var branchName = streamState.Stream.branches.items.First().name;
      //var branch = await client.BranchGet(streamState.Stream.id, branchName, 1);
      var commitId = streamState.Stream.branch.commits.items.FirstOrDefault().referencedObject;
     
      var transport = new ServerTransport(streamState.Client.Account, streamState.Stream.id);

      await Commands.Receive(commitId, streamState, transport);

      /* these lines would cause the libraries in %AppData% to be loaded, including the objects.dll library, which contains types
       * which C# would recognise as being different to the ones used in the converter code
       * */
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      
      Commands.ConvertToNative(converter);

      //Instance.GsaModel.Proxy.NewFile(true);
    }

    private async Task<StreamState> LatestStream(Client client)
    {
      StreamState streamState = null;
      Stream latestStream = null;

      try
      {
        var streams = await client.StreamsGet(1);
        latestStream = streams.First();

        var branches = await client.StreamGetBranches(latestStream.id, 1);
        latestStream.branch = branches.First();
      }
      catch (Exception e)
      {
        
      }

      streamState = new StreamState() { Client = client, Stream = latestStream };
      return streamState;
    }
  }
}
