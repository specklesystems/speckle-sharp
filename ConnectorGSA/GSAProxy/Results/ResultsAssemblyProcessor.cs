using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Results
{
  public class ResultsAssemblyProcessor : ResultsProcessorBase
  {
    public override ResultGroup Group => ResultGroup.Assembly;

    public ResultsAssemblyProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
      List<string> cases = null, List<int> elemIds = null) : base(filePath, unitData, cases, elemIds)
    {
      if (resultTypes == null || resultTypes.Contains(ResultType.AssemblyForcesAndMoments))
      {
        this.resultTypes = new List<ResultType>()
        {
          ResultType.AssemblyForcesAndMoments
        };
      }

      ColumnValuesFns = new Dictionary<ResultType, Func<List<int>, Dictionary<string, object>>>()
      {
        { ResultType.AssemblyForcesAndMoments, ResultTypeColumnValues_AssemblyForcesAndMoments }
      };
    }
    public override bool LoadFromFile(out int numErrorRows, bool parallel = true)
      => base.LoadFromFile<CsvAssembly>(out numErrorRows, parallel);

    #region column_values_fns

    protected Dictionary<string, object> ResultTypeColumnValues_AssemblyForcesAndMoments(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Length);
      var retDict = new Dictionary<string, object>
      {
        { "fx", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Fx, factors)).Cast<object>().ToList() },
        { "fy", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Fy, factors)).Cast<object>().ToList() },
        { "fz", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Fz, factors)).Cast<object>().ToList() },
        { "frc", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Frc, factors)).Cast<object>().ToList() },
        { "mxx", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Mxx, factors)).Cast<object>().ToList() },
        { "myy", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Myy, factors)).Cast<object>().ToList() },
        { "mzz", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Mzz, factors)).Cast<object>().ToList() },
        { "mom", indices.Select(i => ApplyFactors(((CsvAssembly)Records[i]).Mom, factors)).Cast<object>().ToList() },
      };
      return retDict;
    }
    #endregion
  }
}
