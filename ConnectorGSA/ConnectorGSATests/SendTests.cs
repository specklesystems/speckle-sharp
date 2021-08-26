using ConnectorGSA;
using GsaProxy;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConnectorGSATests
{
  public class SendTests : SpeckleConnectorFixture
  {
    private static string testStreamMarker = "Test";
    private static string testStreamDelimiter = "-";

    private async Task<string> GetNewStreamName(Client client, Account account)
    {
      var usersStreamsOnServer = await client.StreamsGet();
      var testStreamNames = usersStreamsOnServer.Where(s => s.name.StartsWith(testStreamMarker)).Select(s => s.name).ToList();
      int testStreamNewNum = 0;
      if (testStreamNames.Count == 0)
      {
        testStreamNewNum = 1;
      }
      else
      {
        var testStreamNums = new List<int>();
        foreach (var n in testStreamNames)
        {
          var pieces = n.Split(new[] { testStreamDelimiter }, StringSplitOptions.RemoveEmptyEntries);
          foreach (var p in pieces)
          {
            if (int.TryParse(p, out int testStreamNum) && testStreamNum > 0)
            {
              testStreamNums.Add(testStreamNum);
              break;
            }
          }
        }
        testStreamNewNum = testStreamNums.Max() + 1;
      }

      return string.Join(testStreamDelimiter, testStreamMarker, testStreamNewNum);
    }

    [Fact]
    public async void SendTest()
    {
      Instance.GsaModel.Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();
      Instance.GsaModel.Layer = GSALayer.Design;
      Instance.GsaModel.Proxy.OpenFile(Path.Combine(TestDataDirectory, modelWithoutResultsFile), true);

      bool loaded = false;
      Base commitObj = null;
      try
      {
        loaded = Commands.LoadDataFromFile();
      }
      catch { }
      finally
      {
        Instance.GsaModel.Proxy.Close();
      }
      //Putting the assert here so that the exception catching can trigger a closing of the GSA file first before any failure of this assertion stops this test
      Assert.True(loaded);

      commitObj = Commands.ConvertToSpeckle();
      Assert.NotNull(commitObj);

      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      var streamState = await NewStream(client, account);
      var transports = new List<ITransport>() { new ServerTransport(streamState.Client.Account, streamState.Stream.id) };

      await Commands.Send(commitObj, streamState, transports);
      
    }

    private async Task<StreamState> NewStream(Client client, Account account)
    {
      var newStreamName = await GetNewStreamName(client, account);
      string streamId = "";
      Speckle.Core.Api.Stream stream = null;
      StreamState streamState = null;

      try
      {
        streamId = await client.StreamCreate(new StreamCreateInput()
        {
          name = newStreamName,
          description = "Stream created as part of automated tests",
          isPublic = false
        });

        stream = await client.StreamGet(streamId);

        streamState = new StreamState() { Client = client, Stream = stream };
      }
      catch (Exception e)
      {
        try
        {
          if (!string.IsNullOrEmpty(streamId))
          {
            await client.StreamDelete(streamId);
          }
        }
        catch
        {
          // POKEMON! (server is prob down)
        }
      }

      return streamState;
    }
  }
}
