using System.Linq;
using Xunit;
using System.IO;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API;
using Speckle.ConnectorGSA.Proxy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ConnectorGSA;
using Speckle.GSA.API.CsvSchema;
using Speckle.GSA.API.GwaSchema;

namespace ConnectorGSATests
{
  public class ProxyTests : SpeckleConnectorFixture
  {
    [Theory]
    [InlineData("SET\tMEMB.8:{speckle_app_id:gh/a}\t5\tTheRest", GwaKeyword.MEMB, 5, "gh/a", "MEMB.8:{speckle_app_id:gh/a}\t5\tTheRest")]
    [InlineData("MEMB.8:{speckle_app_id:gh/a}\t5\tTheRest", GwaKeyword.MEMB, 5, "gh/a", "MEMB.8:{speckle_app_id:gh/a}\t5\tTheRest")]
    [InlineData("SET_AT\t2\tLOAD_2D_THERMAL.2:{speckle_app_id:gh/a}\tTheRest", GwaKeyword.LOAD_2D_THERMAL, 2, "gh/a", "LOAD_2D_THERMAL.2:{speckle_app_id:gh/a}\tTheRest")]
    [InlineData("LOAD_2D_THERMAL.2:{speckle_app_id:gh/a}\tTheRest", GwaKeyword.LOAD_2D_THERMAL, 0, "gh/a", "LOAD_2D_THERMAL.2:{speckle_app_id:gh/a}\tTheRest")]
    public void ParseGwaCommandTests(string gwa, GwaKeyword expKeyword, int expIndex, string expAppId, string expGwaWithoutSet)
    {
      Speckle.ConnectorGSA.Proxy.GsaProxy.ParseGeneralGwa(gwa, out GwaKeyword? keyword, out int? version, out int? foundIndex, 
        out string streamId, out string applicationId, out string gwaWithoutSet, out string keywordAndVersion);

      Assert.Equal(expKeyword, keyword);
      Assert.True(version.HasValue && version.Value > 0);
      Assert.Equal(expIndex, foundIndex ?? 0);
      Assert.Equal(expAppId, applicationId);
      Assert.Equal(expGwaWithoutSet, gwaWithoutSet);
    }

    [Fact]
    public void TestProxyGetDataForCache()
    {
      Instance.GsaModel.StreamLayer = GSALayer.Design;
      var proxy = new GsaProxy();
      proxy.OpenFile(Path.Combine(TestDataDirectory, modelWithoutResultsFile), false);

      var errors = new List<string>();

      var loggingProgress = new Progress<string>();
      loggingProgress.ProgressChanged += (object s, string e) => errors.Add(e);
      Assert.True(proxy.GetGwaData(Instance.GsaModel.StreamLayer, new Progress<string>(), out var records));
      proxy.Close();
      Assert.Empty(errors);
      Assert.Equal(197, records.Count());
    }

    [Fact]
    public void TestDependencyTreeSimple()
    {
      var chars = new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g' };
      var t = new TypeTreeCollection<char>(chars);
      t.Integrate('a', 'c', 'd');
      t.Integrate('c', 'e', 'f');
      var generations = t.Generations();
    }

    [Fact]
    public void TestDependencyTreeComplex()
    {
      var chars = new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g' };
      var t = new TypeTreeCollection<char>(chars);
      t.Integrate('a', 'c', 'd', 'f');
      t.Integrate('c', 'f', 'g');
      t.Integrate('d', 'g');
      t.Integrate('b', 'd', 'e');
      var generations = t.Generations().Select(g => g.OrderBy(v => v).ToList()).ToList();
      Assert.Equal(new[] { 'a', 'b' }, generations[2]);
      Assert.Equal(new[] { 'c', 'd', 'e' }, generations[1]);
      Assert.Equal(new[] { 'f', 'g' }, generations[0]);
    }

    [Fact]
    public void ReadResults()
    {
      Instance.GsaModel.StreamLayer = GSALayer.Both;

      Commands.OpenFile(Path.Combine(TestDataDirectory, modelWithResultsFile), true); //Use a real proxy

      bool loaded = false;
      var resultTypesByGroup = GetResultGroupType();
      var csvRecordsByGroup = resultTypesByGroup.Keys.ToDictionary(g => g,
        g => new List<CsvRecord>());

      try
      {
        loaded = Commands.LoadDataFromFile(null, resultTypesByGroup.Keys, resultTypesByGroup.Keys.SelectMany(g => resultTypesByGroup[g]));
      }
      catch (Exception ex)
      {
      }
      finally
      {
        ((GsaProxy)Instance.GsaModel.Proxy).Close();
      }

      var indices = Instance.GsaModel.Cache.LookupIndices<GsaAssembly>();
      if (indices != null && indices.Count() > 0)
      {
        foreach (var i in indices)
        {
          if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Assembly, i, out var records))
          {
            csvRecordsByGroup[ResultGroup.Assembly].AddRange(records);
          }
        }
      }
      indices = Instance.GsaModel.Cache.LookupIndices<GsaNode>();
      if (indices != null && indices.Count() > 0)
      {
        foreach (var i in indices)
        {
          if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Node, i, out var records))
          {
            csvRecordsByGroup[ResultGroup.Node].AddRange(records);
          }
        }
      }
      indices = Instance.GsaModel.Cache.LookupIndices<GsaEl>();
      if (indices != null && indices.Count() > 0)
      {
        foreach (var i in indices)
        {
          if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element1d, i, out var records))
          {
            csvRecordsByGroup[ResultGroup.Element1d].AddRange(records);
          }
          if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element2d, i, out records))
          {
            csvRecordsByGroup[ResultGroup.Element2d].AddRange(records);
          }
        }
      }

      Assert.True(csvRecordsByGroup.Keys.All(g => csvRecordsByGroup[g].Count > 0));
    }

    [Fact]
    public void TestDeserialisation()
    {
      Instance.GsaModel.Proxy = new GsaProxy();
      ((GsaProxy)Instance.GsaModel.Proxy).OpenFile(saveAsAlternativeFilepath(modelWithoutResultsFile));
      try
      {
        var sid = ((GsaProxy)Instance.GsaModel.Proxy).GetTopLevelSid();
        var ss = JsonConvert.DeserializeObject<List<StreamState>>(sid);
      }
      catch (Exception ex)
      {

      }
      finally
      {
        ((GsaProxy)Instance.GsaModel.Proxy).Close();
      }
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
  }
}
