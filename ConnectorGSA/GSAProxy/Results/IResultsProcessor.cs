using Speckle.GSA.API;
using Speckle.GSA.API.CsvSchema;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Results
{
  public interface IResultsProcessor
  {
    bool LoadFromFile(out int numErrorRows, bool parallel = true);
    bool GetResultRecords(int index, string loadCase, out List<CsvRecord> records);
    bool GetResultRecords(int index, out List<CsvRecord> records);

    string ResultTypeName(ResultType rt);
    List<int> ElementIds { get; }
    List<string> CaseIds { get; }
    ResultGroup Group { get; }
  }
}