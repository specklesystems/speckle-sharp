using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.GSA.API
{
  public abstract class GsaModelBase : IGSAModel
  {
    public GSALayer Layer { get; set; }

    public string Units { get; set; }
    public double CoincidentNodeAllowance { get; set; }
    public List<ResultType> ResultTypes { get; set; }
    public StreamContentConfig StreamSendConfig { get; set; }
    public List<string> ResultCases { get; set; }
    public bool ResultInLocalAxis { get; set; }
    public int Result1DNumPosition { get; set; }

    public char GwaDelimiter { get; set; } = '\t';

    public abstract bool ClearResults(ResultGroup group);

    public abstract List <int> ConvertGSAList(string list, GSAEntity entityType);

    public abstract GsaRecord_ GetNative(GwaKeyword keyword, int index);

    public abstract string GetApplicationId(GwaKeyword keyword, int index);

    public abstract bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1);

    public abstract List<int> LookupIndices(GwaKeyword keyword);

    public abstract int NodeAt(double x, double y, double z, double coincidenceTol);

    public abstract bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null);
  }
}
