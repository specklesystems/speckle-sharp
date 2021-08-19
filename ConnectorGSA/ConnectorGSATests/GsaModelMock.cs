using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectorGSATests
{
  //This class is here mainly to subsitute a mock proxy object that is invoked through normal GsaModel.Instance methods.
  //The cache object calls proxy, but the proxy doesn't call anything else so it is already isolated and able to be tested separately
  public class GsaModelMock : IGSAModel
  {
    public GSALayer Layer { get; set; } = GSALayer.Design;

    public string Units { get; set; } = "mm";
    public double CoincidentNodeAllowance { get; set; } = 0.1;
    public List<ResultType> ResultTypes { get; set; }
    public StreamContentConfig StreamSendConfig { get; set; } = StreamContentConfig.ModelOnly;
    public List<string> ResultCases { get; set; }
    public bool ResultInLocalAxis { get; set; } = true;
    public int Result1DNumPosition { get; set; } = 3;

    public char GwaDelimiter { get => '\t'; }

    public GsaCache cache = new GsaCache();
    public Speckle.ConnectorGSA.Proxy.GsaProxy proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();

    #region mock_fns
    //Default function - just gets the integers from the list where it can
    public Func<string, GSAEntity, List<int>> ConvertGSAListFn = (string l, GSAEntity e) =>
    {
      var retList = new List<int>();
      foreach (var p in l.Split(' '))
      {
        if (int.TryParse(p, out int i))
        {
          retList.Add(i);
        }
      }
      return retList;
    };

    //Need to create delegate types since Func<> doesn't deal with out parameters
    public delegate bool GetResultHierarchyDelegate(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1);
    public delegate bool LoadResultsDelegate(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null);

    public Func<ResultGroup, bool> ClearResultsFn = (ResultGroup rg) => true;
    public GetResultHierarchyDelegate GetResultHierarchyFn =
      (ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension) =>
      {
        valueHierarchy = new Dictionary<string, Dictionary<string, object>>();
        return true;
      };
    public LoadResultsDelegate LoadResultsFn =
      (ResultGroup group, out int numErrorRows, List<string> cases, List<int> elemIds) =>
      {
        numErrorRows = 0;
        return true;
      };
    public Func<double, double, double, double, int> NodeAtFn = (double x, double y, double z, double coincidenceTol) => 1;
    #endregion

    #region cache_related
    public string GetApplicationId<T>(int index) => cache.GetApplicationId<T>(index);

    public GsaRecord GetNative<T>(int index) => cache.GetNative<T>(index, out var gsaRecord) ? gsaRecord : null;

    public List<int> LookupIndices<T>() => cache.LookupIndices<T>().Where(i => i.HasValue && i.Value > 0).Select(i => i.Value).ToList();
    #endregion

    #region proxy_related
    public bool ClearResults(ResultGroup group) 
      => (proxy != null) ? proxy.ClearResults(group) 
      : (ClearResultsFn != null) ? ClearResultsFn(group) : throw new NotImplementedException();

    public List<int> ConvertGSAList(string list, GSAEntity entityType)
      => (proxy != null) ? proxy.ConvertGSAList(list, entityType).ToList() 
      : (ConvertGSAListFn != null) ? ConvertGSAListFn(list, entityType).ToList() : throw new NotImplementedException();

    public bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
      => (proxy != null) ? proxy.GetResultHierarchy(group, index, out valueHierarchy, dimension)
      : (GetResultHierarchyFn != null) ? GetResultHierarchyFn(group, index, out valueHierarchy, dimension) : throw new NotImplementedException();

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
      => (proxy != null) ? proxy.LoadResults(group, out numErrorRows, cases, elemIds)
      : (LoadResultsFn != null) ? LoadResultsFn(group, out numErrorRows, cases, elemIds) : throw new NotImplementedException();

    public int NodeAt(double x, double y, double z, double coincidenceTol) 
      => (proxy != null) ? proxy.NodeAt(x, y, z, coincidenceTol) 
      : (NodeAtFn != null) ? NodeAtFn(x, y, z, coincidenceTol) : throw new NotImplementedException();
    #endregion
  }
}
