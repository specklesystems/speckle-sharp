using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using Speckle.GSA.API.GwaSchema;

namespace GsaProxy
{
  public class GsaModel : IGSAModel
  {
    public GSALayer Layer { get; set; }

    public char GwaDelimiter { get => '\t'; }

    public string Units { get; set; }
    public double CoincidentNodeAllowance { get; set; }
    public List<ResultType> ResultTypes { get; set; }
    public StreamContentConfig StreamSendConfig { get; set; }
    public List<string> ResultCases { get; set; }
    public bool ResultInLocalAxis { get; set; }
    public int Result1DNumPosition { get; set; }

    public bool ClearResults(ResultGroup group)
    {
      throw new NotImplementedException();
    }

    public List<int> ConvertGSAList(string list, GSAEntity entityType)
    {
      throw new NotImplementedException();
    }

    public string GetApplicationId(GwaKeyword keyword, int index)
    {
      throw new NotImplementedException();
    }

    public GsaRecord GetNative(GwaKeyword keyword, int index)
    {
      throw new NotImplementedException();
    }

    public bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
    {
      throw new NotImplementedException();
    }

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
    {
      throw new NotImplementedException();
    }

    public List<int> LookupIndices(GwaKeyword keyword)
    {
      throw new NotImplementedException();
    }

    public int NodeAt(double x, double y, double z, double coincidenceTol)
    {
      throw new NotImplementedException();
    }
  }
}
