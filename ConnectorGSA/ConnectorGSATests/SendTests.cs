using ConnectorGSA;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ConnectorGSATests
{
  //Issues with sending:
  // - members vs elements - i.e. layers
  // - meaningful nodes - if selected, then only nodes with mass or spring properties, or with results, or with application IDs (previously received) should be sent
  // - combining line elements into polylines - including merging of results (fine for lines as they don't interfere with visualisation in the Speckle online viewer)
  // - combining 2D elements into a mesh - only if there are no results for these elements (if there are then leave them as elements)
  // - results - embedded, separated into a different bucket or, if resultsOnly, then no model needs to be sent
  //Other notes:
  // - stream IDs saved with any native object are ignored during sending - it's the send config settings that determine what is sent
  public class SendTests : SpeckleConnectorFixture
  {
    //Sending scenarios

    [Fact]
    public async Task SendDesignLayer()
    {
      //Configure settings for this transmission
      Instance.GsaModel.Layer = GSALayer.Design;

      var memoryTransport = new MemoryTransport();
      var result = await CoordinateSend(converter, memoryTransport);

      Assert.True(result.Loaded);
      Assert.True(result.Converted);
      Assert.True(result.Sent);
      Assert.NotEmpty(result.ConvertedObjects);

      var numExpectedByObjectType = new Dictionary<Type, int>()
      {
        { typeof(Axis), 3 },
        { typeof(Concrete), 1 },
        { typeof(PropertySpring), 8 },
        { typeof(Property1D), 1 },
        { typeof(Property2D), 1 },
        { typeof(Node), 7 },
        { typeof(LoadCase), 3 },
      };

      var objectsByType = result.ConvertedObjects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      foreach (var t in numExpectedByObjectType.Keys)
      {
        Assert.True(objectsByType.ContainsKey(t));
        Assert.NotNull(objectsByType[t]);
        Assert.Equal(numExpectedByObjectType[t], objectsByType[t].Count());
      }
    }

    [Fact]
    public void ReadResults()
    {
      Instance.GsaModel.Layer = GSALayer.Analysis;
      Instance.GsaModel.Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy(); //Use a real proxy
      Instance.GsaModel.Proxy.OpenFile(Path.Combine(TestDataDirectory, modelWithResultsFile), true);

      bool loaded = false;
      var resultTypesByGroup = GetResultGroupType();
      var hierarchiesByResultGroup = resultTypesByGroup.Keys.ToDictionary(g => g, 
        g => new List<Dictionary<string, Dictionary<string, object>>>());

      try
      {
        loaded = Commands.LoadDataFromFile(resultTypesByGroup.Keys, resultTypesByGroup.Keys.SelectMany(g => resultTypesByGroup[g]));
      }
      catch(Exception ex) 
      { 
      }
      finally
      {
        Instance.GsaModel.Proxy.Close();
      }

      var indices = Instance.GsaModel.Cache.LookupIndices<GsaAssembly>();
      if (indices != null && indices.Count() > 0)
      {
        foreach (var i in indices)
        {
          if (Instance.GsaModel.Proxy.GetResultHierarchy(ResultGroup.Assembly, i, out var valueHierarchy))
          {
            hierarchiesByResultGroup[ResultGroup.Assembly].Add(valueHierarchy);
          }
        }
      }
      indices = Instance.GsaModel.Cache.LookupIndices<GsaNode>();
      if (indices != null && indices.Count() > 0)
      {
        foreach (var i in indices)
        {
          if (Instance.GsaModel.Proxy.GetResultHierarchy(ResultGroup.Node, i, out var valueHierarchy))
          {
            hierarchiesByResultGroup[ResultGroup.Node].Add(valueHierarchy);
          }
        }
      }
      indices = Instance.GsaModel.Cache.LookupIndices<GsaEl>();
      if (indices != null && indices.Count() > 0)
      {
        foreach (var i in indices)
        {
          if (Instance.GsaModel.Proxy.GetResultHierarchy(ResultGroup.Element1d, i, out var valueHierarchy, 1))
          {
            hierarchiesByResultGroup[ResultGroup.Element1d].Add(valueHierarchy);
          }
          if (Instance.GsaModel.Proxy.GetResultHierarchy(ResultGroup.Element2d, i, out valueHierarchy, 2))
          {
            hierarchiesByResultGroup[ResultGroup.Element2d].Add(valueHierarchy);
          }
        }
      }

      Assert.True(hierarchiesByResultGroup.Keys.All(g => hierarchiesByResultGroup[g].Count > 0));
    }

    private async Task<CoordinateSendResult> CoordinateSend(ISpeckleConverter converter, params ITransport[] nonServerTransports)
    {
      var result = new CoordinateSendResult();

      Instance.GsaModel.Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy(); //Use a real proxy
      Instance.GsaModel.Proxy.OpenFile(Path.Combine(TestDataDirectory, modelWithoutResultsFile), true);

      result.Loaded = false;
      try
      {
        result.Loaded = Commands.LoadDataFromFile();
      }
      catch { }
      finally
      {
        Instance.GsaModel.Proxy.Close();
      }
      if (!result.Loaded)
      {
        return result;
      }

      var commitObj = Commands.ConvertToSpeckle(converter);
      result.Converted = (commitObj != null);
      if (commitObj == null)
      {
        return result;
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out result.ConvertedObjects));

      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      result.StreamState = await PrepareStream(client);

      result.Sent = await Commands.Send(commitObj, result.StreamState,
        (new List<ITransport> { new ServerTransport(account, result.StreamState.Stream.id) }).Concat(nonServerTransports));

      return result;

    }

    private Dictionary<ResultGroup, List<ResultType>> GetResultGroupType()
    {
      var resultGroups = Enum.GetValues(typeof(ResultGroup)).Cast<ResultGroup>().Where(g => g != ResultGroup.Unknown).ToList();
      var resultTypes = new Dictionary<ResultGroup, List<ResultType>>();
      foreach (var g in resultGroups)
      {
        resultTypes.Add(g, new List<ResultType>());
      }

      foreach (var rt in Enum.GetValues(typeof(ResultType)).Cast<ResultType>())
      {
        var rtStr = rt.ToString();
        if (rtStr.Contains("1d"))
        {
          resultTypes[ResultGroup.Element1d].Add(rt);
        }
        else if (rtStr.Contains("2d"))
        {
          resultTypes[ResultGroup.Element2d].Add(rt);
        }
        else if (rtStr.Contains("Assembly"))
        {
          resultTypes[ResultGroup.Assembly].Add(rt);
        }
        else
        {
          resultTypes[ResultGroup.Node].Add(rt);
        }
      }
      return resultTypes;
    }

    private struct CoordinateSendResult
    {
      public bool Loaded;
      public bool Converted;
      public bool Sent;
      public List<object> ConvertedObjects;
      public StreamState StreamState;
    }

    private async Task<StreamState> PrepareStream(Client client)
    {
      var usersStreamsOnServer = await client.StreamsGet();
      var matchingStreams = usersStreamsOnServer.Where(s => s.name.StartsWith(testStreamMarker));

      Speckle.Core.Api.Stream stream;
      if (matchingStreams.Count() > 0)
      {
        stream = matchingStreams.First();
      }
      else
      {
        stream = await NewStream(client);
      }

      return new StreamState() { Client = client, Stream = stream };
    }

    private async Task<Speckle.Core.Api.Stream> NewStream(Client client)
    {
      string streamId = "";

      try
      {
        streamId = await client.StreamCreate(new StreamCreateInput()
        {
          name = testStreamMarker,
          description = "Stream created as part of automated tests",
          isPublic = false
        });

        return await client.StreamGet(streamId);

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

      return null;
    }
  }
}
