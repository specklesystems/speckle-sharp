using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Results
{
  public class Results2dProcessor : ResultsProcessorBase<CsvElem2dAnnotated>
  {
    public override ResultGroup Group => ResultGroup.Element2d;

    private List<ResultType> possibleResultTypes = new List<ResultType>()
    {
      ResultType.Element2dDisplacement,
      ResultType.Element2dProjectedForce,
      ResultType.Element2dProjectedMoment,
      ResultType.Element2dProjectedStressBottom,
      ResultType.Element2dProjectedStressMiddle,
      ResultType.Element2dProjectedStressTop
    };

    public Results2dProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
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

    protected override bool Scale(CsvElem2dAnnotated record)
    {
      var lengthfactors = GetFactors(ResultUnitType.Length);
      var forceLengthFactors = GetFactors(ResultUnitType.Force, ResultUnitType.Length);
      var stressFactors = GetFactors(ResultUnitType.Stress);

      record.Ux = (resultTypes.Contains(ResultType.Element2dDisplacement)) ? ApplyFactors(record.Ux, lengthfactors) : null;
      record.Uy = (resultTypes.Contains(ResultType.Element2dDisplacement)) ? ApplyFactors(record.Uy, lengthfactors) : null;
      record.Uz = (resultTypes.Contains(ResultType.Element2dDisplacement)) ? ApplyFactors(record.Uz, lengthfactors) : null;

      record.Mx = (resultTypes.Contains(ResultType.Element2dProjectedMoment)) ? ApplyFactors(record.Mx, forceLengthFactors) : null;
      record.My = (resultTypes.Contains(ResultType.Element2dProjectedMoment)) ? ApplyFactors(record.My, forceLengthFactors) : null;
      record.Mxy = (resultTypes.Contains(ResultType.Element2dProjectedMoment)) ? ApplyFactors(record.Mxy, forceLengthFactors) : null;

      record.Nx = (resultTypes.Contains(ResultType.Element2dProjectedForce)) ? ApplyFactors(record.Nx, forceLengthFactors) : null;
      record.Ny = (resultTypes.Contains(ResultType.Element2dProjectedForce)) ? ApplyFactors(record.Ny, forceLengthFactors) : null;
      record.Nxy = (resultTypes.Contains(ResultType.Element2dProjectedForce)) ? ApplyFactors(record.Nxy, forceLengthFactors) : null;
      record.Qx = (resultTypes.Contains(ResultType.Element2dProjectedForce)) ? ApplyFactors(record.Qx, forceLengthFactors) : null;
      record.Qy = (resultTypes.Contains(ResultType.Element2dProjectedForce)) ? ApplyFactors(record.Qy, forceLengthFactors) : null;

      record.Xx_b = (resultTypes.Contains(ResultType.Element2dProjectedStressBottom)) ? ApplyFactors(record.Xx_b, stressFactors) : null;
      record.Yy_b = (resultTypes.Contains(ResultType.Element2dProjectedStressBottom)) ? ApplyFactors(record.Yy_b, stressFactors) : null;
      record.Zz_b = (resultTypes.Contains(ResultType.Element2dProjectedStressBottom)) ? ApplyFactors(record.Zz_b, stressFactors) : null;
      record.Xy_b = (resultTypes.Contains(ResultType.Element2dProjectedStressBottom)) ? ApplyFactors(record.Xy_b, stressFactors) : null;
      record.Yz_b = (resultTypes.Contains(ResultType.Element2dProjectedStressBottom)) ? ApplyFactors(record.Yz_b, stressFactors) : null;
      record.Zx_b = (resultTypes.Contains(ResultType.Element2dProjectedStressBottom)) ? ApplyFactors(record.Zx_b, stressFactors) : null;

      record.Xx_m = (resultTypes.Contains(ResultType.Element2dProjectedStressMiddle)) ? ApplyFactors(record.Xx_m, stressFactors) : null;
      record.Yy_m = (resultTypes.Contains(ResultType.Element2dProjectedStressMiddle)) ? ApplyFactors(record.Yy_m, stressFactors) : null;
      record.Zz_m = (resultTypes.Contains(ResultType.Element2dProjectedStressMiddle)) ? ApplyFactors(record.Zz_m, stressFactors) : null;
      record.Xy_m = (resultTypes.Contains(ResultType.Element2dProjectedStressMiddle)) ? ApplyFactors(record.Xy_m, stressFactors) : null;
      record.Yz_m = (resultTypes.Contains(ResultType.Element2dProjectedStressMiddle)) ? ApplyFactors(record.Yz_m, stressFactors) : null;
      record.Zx_m = (resultTypes.Contains(ResultType.Element2dProjectedStressMiddle)) ? ApplyFactors(record.Zx_m, stressFactors) : null;

      record.Xx_t = (resultTypes.Contains(ResultType.Element2dProjectedStressTop)) ? ApplyFactors(record.Xx_t, stressFactors) : null;
      record.Yy_t = (resultTypes.Contains(ResultType.Element2dProjectedStressTop)) ? ApplyFactors(record.Yy_t, stressFactors) : null;
      record.Zz_t = (resultTypes.Contains(ResultType.Element2dProjectedStressTop)) ? ApplyFactors(record.Zz_t, stressFactors) : null;
      record.Xy_t = (resultTypes.Contains(ResultType.Element2dProjectedStressTop)) ? ApplyFactors(record.Xy_t, stressFactors) : null;
      record.Yz_t = (resultTypes.Contains(ResultType.Element2dProjectedStressTop)) ? ApplyFactors(record.Yz_t, stressFactors) : null;
      record.Zx_t = (resultTypes.Contains(ResultType.Element2dProjectedStressTop)) ? ApplyFactors(record.Zx_t, stressFactors) : null;

      return true;
    }
  }
}
