using ConnectorGSA;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Properties;
using Objects.Structural.Loading;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Results;
using Speckle.ConnectorGSA.Proxy;
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
using System.Threading.Tasks;
using Xunit;

//Note: this project directly references the Objects and ConverterGSA libraries, which the headless tests do not.  This is why they exist
//      in another test project, which doesn't have these references and which uses the Core's own method of loading converters and kits

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
    [Fact]
    public async Task SendDesignLayer() //model only
    {
      //Configure settings for this transmission
      Instance.GsaModel = new GsaModel(); //Use the real thing, not the mock
      Instance.GsaModel.StreamLayer = GSALayer.Design;

      var converter = new ConverterGSA.ConverterGSA();

      var memoryTransport = new MemoryTransport();
      var result = await CoordinateSend(modelWithoutResultsFile, converter, memoryTransport);

      Assert.True(result.Loaded);
      Assert.True(result.Converted);
      Assert.True(result.Sent);
      Assert.Single(result.ConvertedObjectsByStream.Keys);
      Assert.NotEmpty(result.ConvertedObjectsByStream.Values);

      var numExpectedByObjectType = new Dictionary<Type, int>()
      {
        { typeof(Axis), 3 },
        { typeof(GSAConcrete), 1 },
        { typeof(PropertySpring), 8 },
        { typeof(GSAProperty1D), 1 },
        { typeof(GSAProperty2D), 1 },
        { typeof(GSANode), 95 },
        { typeof(GSALoadCase), 3 },
      };

      var convertedObjects = ModelToSingleObjectsList((Base)result.ConvertedObjectsByStream.Values.First());
      var objectsByType = convertedObjects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      foreach (var t in numExpectedByObjectType.Keys)
      {
        Assert.True(objectsByType.ContainsKey(t));
        Assert.NotNull(objectsByType[t]);
        Assert.Equal(numExpectedByObjectType[t], objectsByType[t].Count());
      }
    }

    //Note: this is elements embedded in result objects - not the other way around
    //TO DO - implement the use of the Model objects
    [Fact]
    public async Task SendBothLayersModelOnly()
    {
      //Configure settings for this transmission
      Instance.GsaModel = new GsaModel(); //use the real thing
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      var converter = new ConverterGSA.ConverterGSA();
      var memoryTransport = new MemoryTransport();
      var result = await CoordinateSend(modelWithResultsFile, converter, memoryTransport);

      Assert.True(result.Loaded);
      Assert.True(result.Converted);
      Assert.True(result.Sent);
      Assert.NotEmpty(result.ConvertedObjectsByStream.Values);
      Assert.Equal(2, result.ConvertedObjectsByStream.Keys.Count);

      var numExpectedByObjectType = new Dictionary<Type, int>()
      {
        { typeof(Axis), 3 },
        { typeof(GSAConcrete), 1 },
        { typeof(PropertySpring), 8 },
        { typeof(GSAProperty1D), 1 },
        { typeof(GSAProperty2D), 1 },
        { typeof(GSANode), 3486 },
        { typeof(GSALoadCase), 3 },
        { typeof(GSAElement2D), 3297 },
        { typeof(GSAElement1D), 35 }
      };

      //Just compare analysis objects
      var objects = ModelToSingleObjectsList((Base)result.ConvertedObjectsByStream.Values.ToList()[1]);

      var objectsByType = objects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      foreach (var t in numExpectedByObjectType.Keys)
      {
        Assert.True(objectsByType.ContainsKey(t));
        Assert.NotNull(objectsByType[t]);
        Assert.Equal(numExpectedByObjectType[t], objectsByType[t].Count());
      }
    }

    //TO DO: rename this as SendAnalysisLayerWithResults
    [Fact]
    public async Task SendAnalysisLayerWithSeparateResults()
    {
      //Configure settings for this transmission
      Instance.GsaModel = new GsaModel(); //use the real thing
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.ResultTypes = new List<ResultType>() { ResultType.NodalDisplacements };
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var converter = new ConverterGSA.ConverterGSA();
      var memoryTransport = new MemoryTransport();
      
      
      var result = await CoordinateSend(modelWithResultsFile, converter, memoryTransport);

      Assert.True(result.Loaded);
      Assert.True(result.Converted);
      Assert.True(result.Sent);
      Assert.NotEmpty(result.ConvertedObjectsByStream.Values);
      Assert.Equal(3, result.ConvertedObjectsByStream.Keys.Count);

      var objs = result.ConvertedObjectsByStream.Values.Cast<Base>().ToList();
      var analysisModel = objs[1];
      var resultSetAll = objs[2];
      var objects = ModelToSingleObjectsList(analysisModel);
      var resultObjects = ResultToSingleResultObjectsList(resultSetAll);

      var numExpectedByObjectType = new Dictionary<Type, int>()
      {
        { typeof(Axis), 3 },
        { typeof(GSAConcrete), 1 },
        { typeof(PropertySpring), 8 },
        { typeof(GSAProperty1D), 1 },
        { typeof(GSAProperty2D), 1 },
        { typeof(GSANode), 95 },
        { typeof(GSALoadCase), 3 },
        { typeof(GSAElement2D), 3297 },
        { typeof(GSAElement1D), 35 },
        { typeof(ResultNode), 10 }  //Needs to be reviewed
      };

      var convertedObjects = ModelToSingleObjectsList((Base)result.ConvertedObjectsByStream.Values.ToList()[1]);
      var objectsByType = objects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      foreach (var t in numExpectedByObjectType.Keys)
      {
        Assert.True(objectsByType.ContainsKey(t));
        Assert.NotNull(objectsByType[t]);
        Assert.Equal(numExpectedByObjectType[t], objectsByType[t].Count());
      }
    }

    private List<Base> ModelToSingleObjectsList(Base model)
    {
      var retList = new List<Base>();
      var memberGroups = new string[] { "nodes", "elements", "loads", "restraints", "properties", "materials" };
      foreach (var mg in memberGroups)
      {
        if (model[mg] != null)
        {
          retList.AddRange((List<Base>)model[mg]);
        }
      }
      return retList;
    }

    private List<Base> ResultToSingleResultObjectsList(Base resultSetAll)
    {
      var retList = new List<Base>();
      var memberGroups = new string[] { "resultsNode", "results1D", "results2D" };
      foreach (var mg in memberGroups)
      {
        if (((Base)resultSetAll[mg])[mg] != null)
        {
          retList.AddRange((List<Base>)((Base)resultSetAll[mg])[mg]);
        }
      }
      return retList;
    }

    private async Task<CoordinateSendReturnInfo> CoordinateSend(string testFileName, ISpeckleConverter converter, params ITransport[] nonServerTransports)
    {
      var returnInfo = new CoordinateSendReturnInfo();

      Instance.GsaModel.Proxy = new GsaProxy(); //Use a real proxy
      ((GsaProxy)Instance.GsaModel.Proxy).OpenFile(Path.Combine(TestDataDirectory, testFileName), true);

      if (SendResults())
      {
        Instance.GsaModel.Proxy.PrepareResults(Instance.GsaModel.ResultTypes, Instance.GsaModel.Result1DNumPosition + 2);
        foreach (var rg in Instance.GsaModel.ResultGroups)
        {
          ((GsaProxy)Instance.GsaModel.Proxy).LoadResults(rg, out int numErrorRows);
        }
      }

      returnInfo.Loaded = false;
      try
      {
        returnInfo.Loaded = Commands.LoadDataFromFile(null);
      }
      catch { }
      finally
      {
        ((GsaProxy)Instance.GsaModel.Proxy).Close();
      }
      if (!returnInfo.Loaded)
      {
        return returnInfo;
      }

      var commitObjs = Commands.ConvertToSpeckle(converter);
      returnInfo.Converted = (commitObjs != null && commitObjs.Count > 0);
      if (commitObjs == null)
      {
        return returnInfo;
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var speckleObjects));

      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      returnInfo.ConvertedObjectsByStream = new Dictionary<StreamState, object>();

      foreach (var co in commitObjs)
      {
        var streamState = await PrepareStream(client);

        returnInfo.Sent = await Commands.SendCommit(co, streamState, "",
          (new ITransport[] { new ServerTransport(account, streamState.Stream.id) }).Concat(nonServerTransports).ToArray());

        returnInfo.ConvertedObjectsByStream.Add(streamState, co);
      }
      return returnInfo;

    }

    private struct CoordinateSendReturnInfo
    {
      public bool Loaded;
      public bool Converted;
      public bool Sent;
      public Dictionary<StreamState, object> ConvertedObjectsByStream;
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

    private bool SendResults()
    {
      return ((Instance.GsaModel.StreamSendConfig == StreamContentConfig.ModelAndResults)
        && Instance.GsaModel.ResultTypes != null && Instance.GsaModel.ResultTypes.Count > 0);
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
