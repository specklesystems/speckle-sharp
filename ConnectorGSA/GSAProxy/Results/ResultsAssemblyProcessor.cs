using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Results
{
  public class ResultsAssemblyProcessor : ResultsProcessorBase<CsvAssemblyAnnotated>
  {
    public override ResultGroup Group => ResultGroup.Assembly;

    public ResultsAssemblyProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
      List<string> cases = null, List<int> elemIds = null) : base(filePath, unitData, cases, elemIds)
    {
      if (resultTypes == null || resultTypes.Contains(ResultType.AssemblyForcesAndMoments))
      {
        this.resultTypes = new HashSet<ResultType>
        {
          ResultType.AssemblyForcesAndMoments
        };
      }
    }

    protected override bool Scale(CsvAssemblyAnnotated record)
    {
      //The way this method is written is different to its counterpart in other result processor classes because there is only one result type
      if (!resultTypes.Contains(ResultType.AssemblyForcesAndMoments))
      {
        return false;
      }
      var factors = GetFactors(ResultUnitType.Length);
      record.Fx = ApplyFactors(record.Fx, factors);
      record.Fy = ApplyFactors(record.Fy, factors);
      record.Fz = ApplyFactors(record.Fz, factors);
      record.Mxx = ApplyFactors(record.Mxx, factors);
      record.Myy = ApplyFactors(record.Myy, factors);
      record.Mzz = ApplyFactors(record.Mzz, factors);
      //The rest are calculated, and the scaling should be correct since the other inputs they're based on have been scaled above
      return true;
    }
    
  }
}
