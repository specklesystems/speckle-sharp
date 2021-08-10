using Newtonsoft.Json;
using Speckle.ConnectorGSA.Proxy;
using Speckle.GSA.API;
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
    public void HydrateCache()
    {
      var proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();
      proxy.OpenFile(Path.Combine(TestDataDirectory, modelWithoutResultsFile), false);

      var data = proxy.GetGwaData(DesignLayerKeywords, false);
      var erroredIndices = new List<int>();

      try
      {
        var cache = new GsaCache();

        for (int i = 0; i < data.Count(); i++)
        {
          if (!cache.Upsert(data[i]))
          {
            erroredIndices.Add(i);
          }
        }
      }
      finally
      {
        proxy.Close();
      }

      Assert.Empty(erroredIndices);
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

    [Fact]
    public void ReserveMultipleIndicesSet()
    {
      var cache = new GsaCache();

      cache.Upsert("MEMB", 1, "MEMB.8:{speckle_app_id:Slab0}\t1\tSlab 0\tNO_RGB\tSLAB\t1\t6\t36 37 38 39 40 41 42\t0\t0\t5\tMESH\tLINEAR\t0\t0\t0\t0\t0\tACTIVE\tNO\t0\tALL", "abcdefgh", "Slab0", GwaSetCommandType.Set);

      var newIndices = new List<int>
      {
        cache.ResolveIndex("MEMB", "Slab1"),
        cache.ResolveIndex("MEMB", "Slab2"),
        cache.ResolveIndex("MEMB", "Slab3")
      };

      Assert.Equal(1, cache.LookupIndices("MEMB").Where(i => i.HasValue).Select(i => i.Value).Count());
      Assert.Equal(1, cache.LookupIndices("MEMB", new[] { "Slab0", "Slab1", "Slab2", "Slab3", "Slab4" }).Where(i => i.HasValue).Select(i => i.Value).Count());

      //Try upserting a latest record before converting a provisional to latest
      Assert.True(cache.Upsert("MEMB", 5, "MEMB.8:{speckle_app_id:Slab4}\t5\tSlab 4\tNO_RGB\tSLAB\t1\t6\t36 37 38 39 40 41 42\t0\t0\t5\tMESH\tLINEAR\t0\t0\t0\t0\t0\tACTIVE\tNO\t0\tALL", "abcdefgh", "Slab4", GwaSetCommandType.Set));
      Assert.Equal(2, cache.LookupIndices("MEMB").Where(i => i.HasValue).Select(i => i.Value).Count());
      Assert.Equal(2, cache.LookupIndices("MEMB", new[] { "Slab0", "Slab1", "Slab2", "Slab3", "Slab4" }).Where(i => i.HasValue).Select(i => i.Value).Count());

      //Now convert a provisional to latest and check that the number of records hasn't increased
      Assert.True(cache.Upsert("MEMB", 2, "MEMB.8:{speckle_app_id:Slab1}\t2\tSlab 1\tNO_RGB\tSLAB\t1\t6\t36 37 38 39 40 41 42\t0\t0\t5\tMESH\tLINEAR\t0\t0\t0\t0\t0\tACTIVE\tNO\t0\tALL", "abcdefgh", "Slab1", GwaSetCommandType.Set));
      Assert.Equal(3, cache.LookupIndices("MEMB").Where(i => i.HasValue).Select(i => i.Value).Count());
      Assert.Equal(3, cache.LookupIndices("MEMB", new[] { "Slab0", "Slab1", "Slab2", "Slab3", "Slab4" }).Where(i => i.HasValue).Select(i => i.Value).Count());

      //Check that asking to resolve a previously-created provisional index returns that same one
      Assert.Equal(3, cache.ResolveIndex("MEMB", "Slab2"));

      //Check that the next index recognises (and doesn't re-use) the current provisional indices
      Assert.Equal(6, cache.ResolveIndex("MEMB"));
    }

    [Fact]
    public void ReserveMultipleIndicesSetAt()
    {
      var cache = new GsaCache();

      cache.Upsert("LOAD_2D_THERMAL", 1, "LOAD_2D_THERMAL.2\tGeneral\tG6\t3\tDZ\t239\t509", "abcdefgh", "", GwaSetCommandType.SetAt);

      cache.ResolveIndex("LOAD_2D_THERMAL");
      cache.ResolveIndex("LOAD_2D_THERMAL");

      //Try upserting a latest record before converting a provisional to latest
      Assert.True(cache.Upsert("LOAD_2D_THERMAL", 4, "LOAD_2D_THERMAL.2\tGeneral\tG7\t3\tDZ\t239\t509", "abcdefgh", "Slab4", GwaSetCommandType.Set));
      Assert.Equal(2, cache.LookupIndices("LOAD_2D_THERMAL").Where(i => i.HasValue).Select(i => i.Value).Count());

      //Now convert a provisional to latest and check that the number of records hasn't increased
      Assert.True(cache.Upsert("LOAD_2D_THERMAL", 3, "LOAD_2D_THERMAL.2\tGeneral\tG6 G7 G8 G9 G10\t3\tDZ\t239\t509", "abcdefgh", "Slab1", GwaSetCommandType.Set));
      Assert.Equal(3, cache.LookupIndices("LOAD_2D_THERMAL").Where(i => i.HasValue).Select(i => i.Value).Count());

      //Check that the next index recognises (and doesn't re-use) the current provisional indices
      Assert.Equal(5, cache.ResolveIndex("LOAD_2D_THERMAL"));
    }

    [Fact]
    public void GenerateDesignCache()
    {
      var resources = new GsaAppResources();
      resources.Settings.TargetLayer = GSALayer.Design;

      var senderStreamInfo = new List<SidSpeckleRecord> { new SidSpeckleRecord("testStreamId", "testStream", "testClientId") };

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
          new Progress<string>(), new Progress<double>(), new Progress<SidSpeckleRecord>(), new Progress<SidSpeckleRecord>());

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

