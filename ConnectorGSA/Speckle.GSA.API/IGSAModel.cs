using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;

namespace Speckle.GSA.API
{
  public interface IGSAModel
  {
    //PROPERTIES

    //Settings - general
    GSALayer Layer { get; }
    string Units { get; set; }
    double CoincidentNodeAllowance { get; set; }

    //Settings - results
    List<ResultType> ResultTypes { get; set; }
    StreamContentConfig StreamSendConfig { get; set; }
    List<string> ResultCases { get; set; }
    bool ResultInLocalAxis { get; set; }
    int Result1DNumPosition { get; set; }

    //Cache
    GsaRecord GetNative(GwaKeyword keyword, int index);
    string GetApplicationId(GwaKeyword keyword, int index);
    List<int> LookupIndices(GwaKeyword keyword);

    //Offered by GSA itself
    List<int> ConvertGSAList(string list, GSAEntity entityType);
    int NodeAt(double x, double y, double z, double coincidenceTol);

    char GwaDelimiter { get; }

    //METHODS
    bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null);
    bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1);
    bool ClearResults(ResultGroup group);
  }

  
}
