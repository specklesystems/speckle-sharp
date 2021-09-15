using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.GSA.API
{
  public interface IGSAModel
  {
    //PROPERTIES

    //Settings - general
    GSALayer StreamLayer { get; set; }
    string Units { get; set; }
    double CoincidentNodeAllowance { get; set; }
    int LoggingMinimumLevel { get; set; }
    bool SendOnlyMeaningfulNodes { get; set; }

    //Settings - results
    bool SendResults { get; }
    List<ResultType> ResultTypes { get; set; }
    List<ResultGroup> ResultGroups { get; }
    StreamContentConfig StreamSendConfig { get; set; }
    List<string> ResultCases { get; set; }
    bool ResultInLocalAxis { get; set; }
    int Result1DNumPosition { get; set; }


    IGSACache Cache { get; set; }

    IGSAProxy Proxy { get; set; }
    IGSAMessenger Messenger { get; set; }

    //TEMP
    GsaRecord GetNative<T>(int value);
    List<int> LookupIndices<T>();
    List<int> ConvertGSAList(string v, GSAEntity e);
    string GetApplicationId<T>(int value);
  }

  
}
