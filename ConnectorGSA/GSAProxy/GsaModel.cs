using Speckle.GSA.API;
using System.Collections.Generic;
using Speckle.GSA.API.GwaSchema;
using Speckle.ConnectorGSA.Proxy.Cache;
using System.Linq;
using System;

namespace GsaProxy
{
  public class GsaModel : GsaModelBase
  {
    public static IGSAModel Instance = new GsaModel();

    private static IGSACache cache = new GsaCache();
    private static IGSAProxy proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();

    public override IGSACache Cache { get => cache; set => cache = value; }
    public override IGSAProxy Proxy { get => proxy; set => proxy = value; }

    public GsaModel()
    {
      if (Speckle.GSA.API.Instance.GsaModel == null)
      {
        Speckle.GSA.API.Instance.GsaModel = this;
      }
    }

    /*
    #region cache_related
    public override string GetApplicationId<T>(int index) => Cache.GetApplicationId<T>(index);

    public override GsaRecord GetNative<T>(int index) => Cache.GetNative<T>(index, out var gsaRecord) ? gsaRecord : null;

    public override bool GetNative(Type t, int index, out GsaRecord gsaRecords) => Cache.GetNative(t, index, out gsaRecords);

    public override bool GetNative(Type t, out List<GsaRecord> gsaRecords) => Cache.GetNative(t, out gsaRecords);

    public override bool GetGwaData(bool nodeApplicationIdFilter, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
      => Proxy.GetGwaData(nodeApplicationIdFilter, out records, incrementProgress);

    public override bool SetSpeckleObjects(GsaRecord gsaRecord, IEnumerable<object> speckleObjects) => Cache.SetSpeckleObjects(gsaRecord, speckleObjects);

    public override List<int> LookupIndices<T>() => Cache.LookupIndices<T>().Select(i => i.Value).ToList();
    #endregion

    #region proxy_related
    public override bool ClearResults(ResultGroup group) => Proxy.ClearResults(group);

    public override List<int> ConvertGSAList(string list, GSAEntity entityType) => Proxy.ConvertGSAList(list, entityType).ToList();

    public override bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
      => Proxy.GetResultHierarchy(group, index, out valueHierarchy, dimension);

    public override bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
      => Proxy.LoadResults(group, out numErrorRows, cases, elemIds);

    public override int NodeAt(double x, double y, double z, double coincidenceTol) => Proxy.NodeAt(x, y, z, coincidenceTol);

    


    #endregion
    */
  }
}
