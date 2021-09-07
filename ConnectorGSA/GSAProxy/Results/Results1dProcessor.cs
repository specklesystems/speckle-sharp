using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Results
{
  public class Results1dProcessor : ResultsProcessorBase<CsvElem1dAnnotated>  
  {
    public override ResultGroup Group => ResultGroup.Element1d;


    private List<ResultType> possibleResultTypes = new List<ResultType>()
    {
      ResultType.Element1dDisplacement,
      ResultType.Element1dForce
    };

    public Results1dProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
      List<string> cases = null, List<int> elemIds = null) : base(filePath, unitData, cases, elemIds)
    {
      if (resultTypes == null)
      {
        this.resultTypes = new HashSet<ResultType>(possibleResultTypes);
      }
      else
      {
        this.resultTypes = new HashSet<ResultType>(resultTypes.Intersect(possibleResultTypes));
      }
    }

    protected override bool Scale(CsvElem1dAnnotated record)
    {
      var factors = GetFactors(ResultUnitType.Length);
      var factorsForce = GetFactors(ResultUnitType.Force);
      var factorsMoment = GetFactors(ResultUnitType.Force, ResultUnitType.Length);

      record.Ux = resultTypes.Contains(ResultType.Element1dDisplacement) ? ApplyFactors(record.Ux, factors) : null;
      record.Uy = resultTypes.Contains(ResultType.Element1dDisplacement) ? ApplyFactors(record.Uy, factors) : null;
      record.Uz = resultTypes.Contains(ResultType.Element1dDisplacement) ? ApplyFactors(record.Uz, factors) : null;

      record.Fx = resultTypes.Contains(ResultType.Element1dForce) ? ApplyFactors(record.Fx, factorsForce) : null;
      record.Fy = resultTypes.Contains(ResultType.Element1dForce) ? ApplyFactors(record.Fy, factorsForce) : null;
      record.Fz = resultTypes.Contains(ResultType.Element1dForce) ? ApplyFactors(record.Fz, factorsForce) : null;
      record.Mxx = resultTypes.Contains(ResultType.Element1dForce) ? ApplyFactors(record.Mxx, factorsMoment) : null;
      record.Myy = resultTypes.Contains(ResultType.Element1dForce) ? ApplyFactors(record.Myy, factorsMoment) : null;
      record.Mzz = resultTypes.Contains(ResultType.Element1dForce) ? ApplyFactors(record.Mzz, factorsMoment) : null;

      return true;
    }
  }
}
