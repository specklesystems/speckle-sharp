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
  public class GsaModelMock : GsaModelBase
  {
    public override IGSACache Cache { get; set; } = new GsaCache();
    public override IGSAProxy Proxy { get; set; } = new GsaProxyMock();

    /*
    #region cache_related
    public override string GetApplicationId<T>(int index) => Cache.GetApplicationId<T>(index);

    public override GsaRecord GetNative<T>(int index) => Cache.GetNative<T>(index, out var gsaRecord) ? gsaRecord : null;

    public override bool GetNative(Type t, int index, out GsaRecord gsaRecord) => Cache.GetNative(t, index, out gsaRecord);

    public override bool GetNative(Type t, out List<GsaRecord> gsaRecords) => cache.GetNative(t, out gsaRecords);

    public override bool SetSpeckleObjects(GsaRecord gsaRecords, IEnumerable<object> speckleObjects)
    {
      return true;
    }
    

    public override bool GetGwaData(bool nodeApplicationIdFilter, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
      => proxy.GetGwaData(nodeApplicationIdFilter, out records, incrementProgress);

    public override  List<int> LookupIndices<T>() => cache.LookupIndices<T>();
    #endregion
    */


  }

  public class GsaProxyMock : IGSAProxy
  {
    public GsaProxyMock()  { }

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

    public List<List<Type>> TxTypeDependencyGenerations => throw new NotImplementedException();

    public char GwaDelimiter => throw new NotImplementedException();
    #endregion

    #region proxy_related
    public bool ClearResults(ResultGroup group) => (ClearResultsFn != null) ? ClearResultsFn(group) : throw new NotImplementedException();

    public List<int> ConvertGSAList(string list, GSAEntity entityType)
      => (ConvertGSAListFn != null) ? ConvertGSAListFn(list, entityType).ToList() : throw new NotImplementedException();

    public bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
      => (GetResultHierarchyFn != null) ? GetResultHierarchyFn(group, index, out valueHierarchy, dimension) : throw new NotImplementedException();

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
      => (LoadResultsFn != null) ? LoadResultsFn(group, out numErrorRows, cases, elemIds) : throw new NotImplementedException();

    public int NodeAt(double x, double y, double z, double coincidenceTol)
      => (NodeAtFn != null) ? NodeAtFn(x, y, z, coincidenceTol) : throw new NotImplementedException();

    public bool OpenFile(string path, bool showWindow = true, object gsaInstance = null) => true;

    public bool GetGwaData(bool nodeApplicationIdFilter, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
    {
      records = new List<GsaRecord>();
      return true;
    }

    public void Close()
    {
    }

    #endregion
  }
}
