using Speckle.ConnectorGSA.Proxy;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ConnectorGSATests
{
  public class CacheTests : SpeckleConnectorFixture
  {
    [Fact]
    public void SpeckleObjectTest()
    {
      
    }

    [Fact]
    public void HydrateCache()
    {
      Instance.GsaModel.StreamLayer = GSALayer.Design;
      proxy = new GsaProxy();
      var errored = new Dictionary<int, GsaRecord>();
      var errors = new List<string>();
      var loggingProgress = new Progress<string>();
      loggingProgress.ProgressChanged += (object o, string e) => errors.Add(e);
      try
      {
        ((GsaProxy)proxy).OpenFile(Path.Combine(TestDataDirectory, modelWithoutResultsFile), false);

        Assert.True(((GsaProxy)proxy).GetGwaData(Instance.GsaModel.StreamLayer, loggingProgress, out var records));

        for (int i = 0; i < records.Count(); i++)
        {
          if (!cache.Upsert(records[i]))
          {
            errored.Add(i, records[i]);
          }
        }
      }
      finally
      {
        ((GsaProxy)Instance.GsaModel.Proxy).Close();
      }
      Assert.Empty(errors);
      Assert.Empty(errored);
    }

    /*
    [Theory]
    [InlineData(false, "A1, A2 A2 to A3;C1-\nA2,C2to A1 and C2 to C10", 3, 2)]
    [InlineData(false, "A1c1", 1, 1)]
    [InlineData(false, "All all", 3, 4)]
    public void LoadCaseNameTest(bool withResults, string testCaseString, int expectedAs, int expectedCs)
    {
      var filename = withResults ? modelWithoutResultsFile : modelWithoutResultsFile;

      var gsaProxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();
      gsaProxy.OpenFile(Path.Combine(TestDataDirectory, filename), false);
      var data = gsaProxy.GetGwaData(new List<string> { "ANAL", "COMBINATION" }, false);
      gsaProxy.Close();

      var cache = new GsaCache();
      foreach (var r in data)
      {
        cache.Upsert(r.Keyword, r.Index, r.GwaWithoutSet, r.StreamId, r.ApplicationId, r.GwaSetType);
      }

      var expandedLoadCases = cache.ExpandLoadCasesAndCombinations(testCaseString);

      Assert.Equal(expectedAs, expandedLoadCases.Where(c => char.ToLowerInvariant(c[0]) == 'a').Count());
      Assert.Equal(expectedCs, expandedLoadCases.Where(c => char.ToLowerInvariant(c[0]) == 'c').Count());
    }
    */

    [Fact]
    public void ReserveMultipleIndicesSet()
    {
      var parser = new GsaMembParser();
      //MEMB.8:{speckle_app_id:Slab0}\t6\tSlab 0\tNO_RGB\tSLAB\tALL\t1\t6\t36 37 38 39 40 41 42\t0\t0\t5\tYES\tLINEAR\t0\t0\t0\t0\t0\t0\tACTIVE\t0\tNO\tREBAR_2D.1\t0.03\t0.03\t0
      //Assert.True(parser.FromGwa("MEMB.8:{speckle_app_id:Slab0}\t1\tSlab 0\tNO_RGB\tSLAB\t1\t6\t36 37 38 39 40 41 42\t0\t0\t5\tMESH\tLINEAR\t0\t0\t0\t0\t0\tACTIVE\tNO\t0\tALL"));
      Assert.True(parser.FromGwa("MEMB.8:{speckle_app_id:Slab0}\t6\tSlab 0\tNO_RGB\tSLAB\tALL\t1\t6\t36 37 38 39 40 41 42\t0\t0\t5\tYES\tLINEAR\t0\t0\t0\t0\t0\t0\tACTIVE\t0\tNO\tREBAR_2D.1\t0.03\t0.03\t0"));
      parser.Record.StreamId = "abcdefgh";
      cache.Upsert(parser.Record);

      var newIndices = new List<int>
      {
        cache.ResolveIndex<GsaMemb>("Slab1"),
        cache.ResolveIndex<GsaMemb>("Slab2"),
        cache.ResolveIndex<GsaMemb>("Slab3")
      };

      Assert.Single(cache.LookupIndices<GsaMemb>());
      Assert.Single(cache.LookupIndices<GsaMemb>( new[] { "Slab0", "Slab1", "Slab2", "Slab3", "Slab4" }).Where(i => i.HasValue).Select(i => i.Value));

      //Try upserting a latest record before converting a provisional to latest

      parser = new GsaMembParser();
      Assert.True(parser.FromGwa("MEMB.8:{speckle_app_id:Slab4}\t5\tSlab 4\tNO_RGB\tSLAB\tALL\t1\t10\t64 65 66 67 68 69 70\t0\t0\t5\tYES\tLINEAR\t0\t0\t0\t0\t0\t0\tACTIVE\t0\tNO\tREBAR_2D.1\t0.03\t0.03\t0"));
      parser.Record.StreamId = "abcdefgh";
      Assert.True(cache.Upsert(parser.Record));
      Assert.Equal(2, cache.LookupIndices<GsaMemb>().Count());
      Assert.Equal(2, cache.LookupIndices<GsaMemb>(new[] { "Slab0", "Slab1", "Slab2", "Slab3", "Slab4" }).Where(i => i.HasValue).Select(i => i.Value).Count());

      parser = new GsaMembParser();
      //Now convert a provisional to latest and check that the number of records hasn't increased
      Assert.True(parser.FromGwa("MEMB.8:{speckle_app_id:Slab1}\t2\tSlab 1\tNO_RGB\tSLAB\tALL\t1\t7\t43 44 45 46 47 48 49\t0\t0\t5\tYES\tLINEAR\t0\t0\t0\t0\t0\t0\tACTIVE\t0\tNO\tREBAR_2D.1\t0.03\t0.03\t0"));
      //Note: the index here (2) will class with what should have been provisionally created for slab2.  So the cache should let this new upsert record
      //claim index 2, and what was provisionally created at that index should be moved to the first free index, which now would be 1
      parser.Record.StreamId = "abcdefgh";
      Assert.True(cache.Upsert(parser.Record));

      Assert.Equal(3, cache.LookupIndices<GsaMemb>().Count());
      Assert.Equal(3, cache.LookupIndices<GsaMemb>( new[] { "Slab0", "Slab1", "Slab2", "Slab3", "Slab4" }).Where(i => i.HasValue).Select(i => i.Value).Count());

      //Check that asking to resolve a previously-created provisional index returns that same one
      Assert.Equal(1, cache.ResolveIndex<GsaMemb>("Slab2"));

      //Check that the next index recognises (and doesn't re-use) the current provisional indices
      Assert.Equal(4, cache.ResolveIndex<GsaMemb>());
    }

    [Fact]
    public void ReserveMultipleIndicesSetAt()
    {
      var parser = new GsaLoad2dThermalParser();
      Assert.True(parser.FromGwa("LOAD_2D_THERMAL.2\tGeneral\tG6\t3\tDZ\t239\t509"));
      parser.Record.Index = 1;
      parser.Record.StreamId = "abcdefgh";
      Assert.True(cache.Upsert(parser.Record));

      cache.ResolveIndex<GsaLoad2dThermal>();
      cache.ResolveIndex<GsaLoad2dThermal>();

      //Try upserting a latest record before converting a provisional to latest
      parser = new GsaLoad2dThermalParser();
      Assert.True(parser.FromGwa("LOAD_2D_THERMAL.2\tGeneral\tG7\t3\tDZ\t239\t509"));
      parser.Record.Index = 4;
      parser.Record.StreamId = "abcdefgh";
      Assert.True(cache.Upsert(parser.Record));
      Assert.Equal(2, cache.LookupIndices<GsaLoad2dThermal>().Count());

      //Now convert a provisional to latest and check that the number of records hasn't increased
      parser = new GsaLoad2dThermalParser();
      Assert.True(parser.FromGwa("LOAD_2D_THERMAL.2\tGeneral\tG6 G7 G8 G9 G10\t3\tDZ\t239\t509"));
      parser.Record.Index = 3;
      parser.Record.StreamId = "abcdefgh";
      Assert.True(cache.Upsert(parser.Record));
      Assert.Equal(3, cache.LookupIndices<GsaLoad2dThermal>().Count());

      //Check that the next index recognises (and doesn't re-use) the current provisional indices
      Assert.Equal(5, cache.ResolveIndex<GsaLoad2dThermal>());
    }

    /*
    [Fact]
    public void GenerateDesignCache()
    {
      var resources = new GsaAppResources();
      resources.Settings.TargetLayer = GSALayer.Design;

      var senderStreamInfo = new List<StreamState> { new StreamState("testStreamId", "testStream", "testClientId") };

      //This runs SpeckleInitializer.Initialize() and fills WriteTypePrereqs and ReadTypePrereqs
      GSA.Init("");

      Status.StatusChanged += (s, e) => Debug.WriteLine("Status: " + e.Name);

      var filePath = @"C:\Users\Nic.Burgers\OneDrive - Arup\Issues\Nguyen Le\2D result\shear wall system-seismic v10.1.gwb";

      GSA.App.Proxy.OpenFile(Path.Combine(TestDataDirectory, filePath), false);

      var senderCoordinator = new SenderCoordinator();
      bool failed = false;
      try
      {

        //This will load data from all streams into the cache
        senderCoordinator.Initialize("", "", senderStreamInfo, (restApi, apiToken) => new TestSpeckleGSASender(), new Progress<MessageEventArgs>(),
          new Progress<string>(), new Progress<double>(), new Progress<StreamState>(), new Progress<StreamState>());

        _ = senderCoordinator.Trigger();

        //Each kit stores their own objects to be sent
        var speckleObjects = GSA.GetSpeckleObjectsFromSenderDictionaries();
        var response = new ResponseObject() { Resources = speckleObjects };
        var jsonToWrite = JsonConvert.SerializeObject(response, Formatting.Indented);

        Helper.WriteFile(jsonToWrite, designLayerExpectedFile, TestDataDirectory);
      }
      catch
      {
        failed = true;
      }
      finally
      {
        GSA.App.Proxy.Close();
      }
      Assert.False(failed);
    }
    */
  }
}

