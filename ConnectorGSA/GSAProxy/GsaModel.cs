using Speckle.GSA.API;
using System.Collections.Generic;
using Speckle.GSA.API.GwaSchema;
using Speckle.ConnectorGSA.Proxy.Cache;
using System.Linq;

namespace GsaProxy
{
  public class GsaModel : IGSAModel
  {
    public static IGSAModel Instance = new GsaModel();

    private static readonly GsaCache Cache = new GsaCache();
    private static readonly Speckle.ConnectorGSA.Proxy.GsaProxy Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();

    public GSALayer Layer { get; set; } = GSALayer.Design;

    public char GwaDelimiter { get => '\t'; }

    public string Units { get; set; } = "mm";
    public double CoincidentNodeAllowance { get; set; }
    public List<ResultType> ResultTypes { get; set; }
    public StreamContentConfig StreamSendConfig { get; set; }
    public List<string> ResultCases { get; set; }
    public bool ResultInLocalAxis { get; set; }
    public int Result1DNumPosition { get; set; } = 3;

    public GsaModel()
    {
      if (Speckle.GSA.API.Instance.GsaModel == null)
      {
        Speckle.GSA.API.Instance.GsaModel = this;
      }
    }

    #region cache_related
    public string GetApplicationId<T>(int index) => Cache.GetApplicationId<T>(index);

    public GsaRecord GetNative<T>(int index) => Cache.GetNative<T>(index, out var gsaRecord) ? gsaRecord : null;

    public List<int> LookupIndices<T>() => Cache.LookupIndices<T>().Where(i => i.HasValue && i.Value > 0).Select(i => i.Value).ToList();
    #endregion

    #region proxy_related
    public bool ClearResults(ResultGroup group) => Proxy.ClearResults(group);

    public List<int> ConvertGSAList(string list, GSAEntity entityType) => Proxy.ConvertGSAList(list, entityType).ToList();

    public bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
      => Proxy.GetResultHierarchy(group, index, out valueHierarchy, dimension);

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
      => Proxy.LoadResults(group, out numErrorRows, cases, elemIds);

    public int NodeAt(double x, double y, double z, double coincidenceTol) => Proxy.NodeAt(x, y, z, coincidenceTol);
    #endregion
  }
}
