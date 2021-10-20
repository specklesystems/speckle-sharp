using Speckle.GSA.API.CsvSchema;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.GSA.API
{
  public interface IGSAProxy
  {
    //Just the methods needed for the kit(s)
    List<List<Type>> GetTxTypeDependencyGenerations(GSALayer layer);

    //Offered by GSA itself
    List<int> ConvertGSAList(string list, GSAEntity entityType);
    int NodeAt(double x, double y, double z, double coincidenceTol);

    char GwaDelimiter { get; }

    //METHODS
    void CalibrateNodeAt();
    bool PrepareResults(IEnumerable<ResultType> resultTypes, int numBeamPoints = 3);
    bool GetResultRecords(ResultGroup group, int index, out List<CsvRecord> records);
    bool GetResultRecords(ResultGroup group, int index, string loadCase, out List<CsvRecord> records);
  }
}
