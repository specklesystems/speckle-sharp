using ConnectorGSA;
using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.GSA.API;
using Speckle.GSA.API.CsvSchema;
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

    //public override IGSAMessenger Messenger { get; set; } = new GsaMessenger();
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
    public delegate bool GetResultRecordsDelegate(ResultGroup group, int index, out List<CsvRecord> Records);
    public delegate bool GetResultRecordsByLoadCaseDelegate(ResultGroup group, int index, string loadCase, out List<CsvRecord> Records);
    public delegate bool LoadResultsDelegate(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null);

    public GetResultRecordsDelegate GetResultRecordsFn =
      (ResultGroup group, int index, out List<CsvRecord> records) =>
      {
        records = new List<CsvRecord>();
        return true;
      };
    public GetResultRecordsByLoadCaseDelegate GetResultRecordsByLoadCaseFn =
      (ResultGroup group, int index, string loadCase, out List<CsvRecord> records) =>
      {
        records = new List<CsvRecord>();
        return true;
      };
    public LoadResultsDelegate LoadResultsFn =
      (ResultGroup group, out int numErrorRows, List<string> cases, List<int> elemIds) =>
      {
        numErrorRows = 0;
        return true;
      };
    public Func<ResultGroup, bool> ClearResultsFn = (ResultGroup rg) => true;
    public Func<double, double, double, double, int> NodeAtFn = (double x, double y, double z, double coincidenceTol) => 1;

    public List<List<Type>> TxTypeDependencyGenerations => throw new NotImplementedException();

    public char GwaDelimiter => '\t';
    #endregion

    #region proxy_related

    public bool NewFile(bool showWindow = true, object gsaInstance = null) => true;

    public bool OpenFile(string path, bool showWindow = true, object gsaInstance = null) => true;

    public bool GetGwaData(GSALayer layer, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
    {
      records = new List<GsaRecord>();
      return true;
    }
    public bool PrepareResults(IEnumerable<ResultType> resultTypes, int numBeamPoints = 3) => true;

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
      => (LoadResultsFn != null) ? LoadResultsFn(group, out numErrorRows, cases, elemIds) : throw new NotImplementedException();

    public bool GetResultRecords(ResultGroup group, int index, out List<CsvRecord> records)
      => (GetResultRecordsFn != null) ? GetResultRecordsFn(group, index, out records) : throw new NotImplementedException();

    public bool GetResultRecords(ResultGroup group, int index, string loadCase, out List<CsvRecord> records)
      => (GetResultRecordsByLoadCaseFn != null) ? GetResultRecordsByLoadCaseFn(group, index, loadCase, out records) : throw new NotImplementedException();

    public bool ClearResults(ResultGroup group) => (ClearResultsFn != null) ? ClearResultsFn(group) : throw new NotImplementedException();

    public List<int> ConvertGSAList(string list, GSAEntity entityType)
      => (ConvertGSAListFn != null) ? ConvertGSAListFn(list, entityType).ToList() : throw new NotImplementedException();

    public int NodeAt(double x, double y, double z, double coincidenceTol)
      => (NodeAtFn != null) ? NodeAtFn(x, y, z, coincidenceTol) : throw new NotImplementedException();

    public string GenerateApplicationId(Type schemaType, int gsaIndex) => "";

    public void Close() { }

    public void CalibrateNodeAt() { }

    public bool SaveAs(string filePath) => true;

    public bool Clear() => true;

    public string GetTopLevelSid() => "";
    public bool SetTopLevelSid(string StreamState) => true;

    public bool Save() => true;

    public List<List<Type>> GetTxTypeDependencyGenerations(GSALayer layer) => new List<List<Type>>();

    public void WriteModel(List<GsaRecord> gsaRecords, GSALayer layer) { }

    public List<Type> GetNodeDependentTypes(GSALayer layer) => new List<Type>();

    #endregion
  }
}
