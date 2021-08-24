using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConverterGSATests
{
  public class GsaModelMock : GsaModelBase
  {
    public override IGSACache Cache { get; set; } = new GsaCache();
    public override IGSAProxy Proxy { get; set; } = new GsaProxyMock();
  }

  internal class GsaProxyMock : IGSAProxy
    {
    //Assign these in each test to control what the methods below (called by the kit) return
    public Func<string, GSAEntity, List<int>> ConverterGSAListFn;
    public Func<double, double, double, int> NodeAtFn;
    public Dictionary<GwaKeyword, List<int>> IndicesByKeyword;
    public Dictionary<GwaKeyword, Dictionary<int, string>> ApplicationIdsByKeywordId;
    public Dictionary<GwaKeyword, Dictionary<int, GsaRecord>> NativesByKeywordId;

    protected Dictionary<ResultGroup, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> resultsData;
    protected Dictionary<GwaKeyword, Type> TypesByKeyword;
    protected Dictionary<Type, GwaKeyword> KeywordsByType;

    public List<List<Type>> TxTypeDependencyGenerations => throw new NotImplementedException();

    public char GwaDelimiter => throw new NotImplementedException();

    public GsaProxyMock()
    {
      PopulateTypesKeywords();
    }

    
    public bool GetGwaData(bool nodeApplicationIdFilter, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
    {
      records = null;
      return true;
    }

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
    {
      numErrorRows = 0;
      return true;
    }

    public bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
    {
      valueHierarchy = resultsData[group][index];
      return true;
    }

    public bool ClearResults(ResultGroup group) => true;

    protected bool PopulateTypesKeywords()
    {
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
    }

      

    #region test_config_fns
    public bool AddResultData(ResultGroup group, int index, Dictionary<string, Dictionary<string, object>> valueHierarchy)
    {
      if (resultsData == null)
      {
        resultsData = new Dictionary<ResultGroup, Dictionary<int, Dictionary<string, Dictionary<string, object>>>>();
      }
      if (!resultsData.ContainsKey(group))
      {
        resultsData.Add(group, new Dictionary<int, Dictionary<string, Dictionary<string, object>>>());
      }
      if (!resultsData[group].ContainsKey(index))
      {
        resultsData[group].Add(index, new Dictionary<string, Dictionary<string, object>>());
      }
      resultsData[group][index] = valueHierarchy;
      return true;
    }

    public void Clear()
    {
      ConverterGSAListFn = null;
      NodeAtFn = null;
      IndicesByKeyword = null;
      ApplicationIdsByKeywordId = null;
      NativesByKeywordId = null;
    }

    public bool OpenFile(string path, bool showWindow = true, object gsaInstance = null)
    {
      return true;
    }

    public List<int> ConvertGSAList(string list, GSAEntity entityType)
    {
      return new List<int>() { 1 };
    }

    public int NodeAt(double x, double y, double z, double coincidenceTol)
    {
      return 1;
    }

    public void Close()
    {
      
    }

    #endregion
  }
}
