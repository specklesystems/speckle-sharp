using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace ConverterGSATests
{
  public class GsaModelMock : GsaModelBase
  {
    //Assign these in each test to control what the methods below (called by the kit) return
    public Func<string, GSAEntity, List<int>> ConverterGSAListFn;
    public Func<double, double, double, int> NodeAtFn;
    public Dictionary<GwaKeyword, List<int>> IndicesByKeyword;
    public Dictionary<GwaKeyword, Dictionary<int, string>> ApplicationIdsByKeywordId;
    public Dictionary<GwaKeyword, Dictionary<int, GsaRecord_>> NativesByKeywordId;

    protected Dictionary<ResultGroup, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> resultsData;

    #region interface_fns
    //Assumption: don't need coincidenceTol for testing
    public override int NodeAt(double x, double y, double z, double coincidenceTol) => NodeAtFn(x, y, z);

    public override List<int> ConvertGSAList(string list, GSAEntity entityType) => ConverterGSAListFn(list, entityType);

    public override List<int> LookupIndices(GwaKeyword keyword) => IndicesByKeyword[keyword];

    public override string GetApplicationId(GwaKeyword keyword, int index) => ApplicationIdsByKeywordId[keyword][index];

    public override GsaRecord_ GetNative(GwaKeyword keyword, int index) => NativesByKeywordId[keyword][index];

    public override bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
    {
      numErrorRows = 0;
      return true;
    }

    public override bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
    {
      valueHierarchy = resultsData[group][index];
      return true;
    }

    public override bool ClearResults(ResultGroup group) => true;
    #endregion

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

    #endregion
  }
}
