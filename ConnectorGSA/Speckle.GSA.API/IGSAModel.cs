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
    List<ResultGroup> ResultGroups   { get; } // Set when ResultTypes are set
    StreamContentConfig StreamSendConfig { get; set; }
    List<string> ResultCases { get; set; }
    bool ResultInLocalAxis { get; set; }
    int Result1DNumPosition { get; set; }


    IGSACache Cache { get; set; }

    IGSAProxy Proxy { get; set; }
    //IGSAMessenger Messenger { get; set; }
    IProgress<bool> ConversionProgress { get; set; }

    //List<List<Type>> SpeckleDependencyTree();
  }

  
}
