using Speckle.ConnectorGSA.Proxy;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API;
using Speckle.GSA.API.CsvSchema;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConverterGSATests
{
  internal class GsaProxyMockForConverterTests : IGSAProxy
  {
    //Assign these in each test to control what the methods below (called by the kit) return
    public Func<string, GSAEntity, List<int>> ConverterGSAListFn;
    public Func<double, double, double, int> NodeAtFn;
    public Dictionary<GwaKeyword, List<int>> IndicesByKeyword;
    public Dictionary<GwaKeyword, Dictionary<int, string>> ApplicationIdsByKeywordId;
    public Dictionary<GwaKeyword, Dictionary<int, GsaRecord>> NativesByKeywordId;

    protected Dictionary<ResultGroup, Dictionary<int, Dictionary<string, List<CsvRecord>>>> resultsData;
    //protected Dictionary<GwaKeyword, Type> TypesByKeyword;
    //protected Dictionary<Type, GwaKeyword> KeywordsByType;
    
    public char GwaDelimiter => throw new NotImplementedException();

    public GsaProxyMockForConverterTests()
    {
      PopulateTypesKeywords();
    }

    public bool NewFile(bool showWindow = true, object gsaInstance = null) => true;
    public bool OpenFile(string path, bool showWindow = true, object gsaInstance = null) => true;

    public List<int> ConvertGSAList(string list, GSAEntity entityType) => new List<int>() { 1 };

    public int NodeAt(double x, double y, double z, double coincidenceTol) => (NodeAtFn == null) ? 1 : NodeAtFn(x, y, z);

    public void Close() { }

    public string GenerateApplicationId(Type schemaType, int gsaIndex) => "";

    public void Clear()
    {
      ConverterGSAListFn = null;
      NodeAtFn = null;
      IndicesByKeyword = null;
      ApplicationIdsByKeywordId = null;
      NativesByKeywordId = null;
    }

    public bool GetGwaData(GSALayer layer, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
    {
      records = null;
      return true;
    }

    #region results
    public bool PrepareResults(IEnumerable<ResultType> resultTypes, int numBeamPoints = 3) => true;

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
    {
      numErrorRows = 0;
      return true;
    }

    public bool GetResultRecords(ResultGroup group, int index, out List<CsvRecord> records)
    {
      if (resultsData != null && resultsData.ContainsKey(group) && resultsData[group].ContainsKey(index))
      {
        records = resultsData[group][index].SelectMany(kvp => kvp.Value).ToList();
        return true;
      }
      records = null;
      return false;
    }

    public bool GetResultRecords(ResultGroup group, int index, string loadCase, out List<CsvRecord> records)
    {
      if (resultsData.ContainsKey(group) && resultsData[group].ContainsKey(index) && resultsData[group][index].ContainsKey(loadCase))
      {
        records = resultsData[group][index][loadCase];
        return true;
      }
      records = null;
      return false;
    }

    public bool ClearResults(ResultGroup group)
    {
      if (resultsData.ContainsKey(group))
      {
        resultsData[group].Clear();
        resultsData.Remove(group);
      }
      return true;
    }

    #endregion

    protected bool PopulateTypesKeywords()
    {
      /*  there is no reference to ConnectorGSA, which would be needed here
      try
      {
        var gwaParserType = typeof(IGwaParser);
        var assembly = gwaParserType.Assembly; //This assembly
        var assemblyTypes = assembly.GetTypes().ToList();

        var gsaBaseType = typeof(GwaParser<GsaRecord>);
        var gsaAttributeType = typeof(GsaType);

        var parserTypes = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, gwaParserType)
          && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)
          && Helper.IsSelfContained(t)
          && !t.IsAbstract
          ).ToList();

        var gwaParserInterface = typeof(IGwaParser);

        this.TypesByKeyword = new Dictionary<GwaKeyword, Type>();
        this.KeywordsByType = new Dictionary<Type, GwaKeyword>();

        if (parserTypes.Any(t => !t.InheritsOrImplements(gwaParserInterface)))
        {
          return false;
        }

        foreach (var pt in parserTypes)
        {
          var kw = Helper.GetGwaKeyword(pt);
          var t = pt.BaseType.GetGenericArguments().First();
          this.TypesByKeyword.Add(kw, t);
          this.KeywordsByType.Add(t, kw);
        }

        return true;
      }
      catch
      {
        return false;
      }
      */
      return true;
    }

    public List<List<Type>> GetTxTypeDependencyGenerations(GSALayer layer)
    {
      var proxy = new GsaProxy();
      return proxy.GetTxTypeDependencyGenerations(layer);
    }

    #region test_config_fns
    public bool AddResultData(ResultGroup group, List<CsvRecord> records)
    {
      if (resultsData == null)
      {
        resultsData = new Dictionary<ResultGroup, Dictionary<int, Dictionary<string, List<CsvRecord>>>>();
      }
      if (!resultsData.ContainsKey(group))
      {
        resultsData.Add(group, new Dictionary<int, Dictionary<string, List<CsvRecord>>>());
      }

      var recordsByIndexLoadCase = records.GroupBy(r => r.ElemId).ToDictionary(g1 => g1.Key, g1 => g1.GroupBy(gr => gr.CaseId)
      .ToDictionary(g2 => g2.Key, g2 => g2.ToList()));

      foreach (var index in recordsByIndexLoadCase.Keys)
      {
        if (!resultsData[group].ContainsKey(index))
        {
          resultsData[group].Add(index, new Dictionary<string, List<CsvRecord>>());
        }
        resultsData[group][index] = recordsByIndexLoadCase[index];
      }
      
      return true;
    }

    public void CalibrateNodeAt() { }

    public bool SaveAs(string filePath) => true;

    public string GetTopLevelSid() => "";

    public bool SetTopLevelSid(string StreamState) => true;

    public bool Save() => true;

    public void WriteModel(List<GsaRecord> gsaRecords, GSALayer layer)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
