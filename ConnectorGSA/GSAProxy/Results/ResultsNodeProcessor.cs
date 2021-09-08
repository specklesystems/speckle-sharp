using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Results
{
  public class ResultsNodeProcessor : ResultsProcessorBase<CsvNodeAnnotated>
  {
    public override ResultGroup Group => ResultGroup.Node;

    private List<ResultType> possibleResultTypes = new List<ResultType>()
    {
      ResultType.NodalDisplacements,
      ResultType.NodalVelocity,
      ResultType.NodalAcceleration,
      ResultType.NodalReaction,
      ResultType.ConstraintForces
    };

    public ResultsNodeProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
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

    protected override bool Scale(CsvNodeAnnotated record)
    {
      var factorsLength = GetFactors(ResultUnitType.Length);
      var factorsRotation = GetFactors(ResultUnitType.Angle);
      var factorsAccel = GetFactors(ResultUnitType.Accel);
      var factorsForce = GetFactors(ResultUnitType.Force);
      var factorsForceLength = GetFactors(ResultUnitType.Force, ResultUnitType.Length);
      var factorsLengthTime = GetFactors(ResultUnitType.Length, ResultUnitType.Time);

      record.Ux = resultTypes.Contains(ResultType.NodalDisplacements) ? ApplyFactors(record.Ux, factorsLength) : null;
      record.Ux = resultTypes.Contains(ResultType.NodalDisplacements) ? ApplyFactors(record.Uy, factorsLength) : null;
      record.Ux = resultTypes.Contains(ResultType.NodalDisplacements) ? ApplyFactors(record.Uz, factorsLength) : null;
      record.Ux = resultTypes.Contains(ResultType.NodalDisplacements) ? ApplyFactors(record.Rxx, factorsLength) : null;
      record.Ux = resultTypes.Contains(ResultType.NodalDisplacements) ? ApplyFactors(record.Ryy, factorsLength) : null;
      record.Ux = resultTypes.Contains(ResultType.NodalDisplacements) ? ApplyFactors(record.Rzz, factorsLength) : null;
      //TO DO: review if |r| needs scaling even though it's calculated - it seems to use rotation factor, which isn't part of its inputs

      record.Vx = resultTypes.Contains(ResultType.NodalVelocity) ? ApplyFactors(record.Vx, factorsLengthTime) : null;
      record.Vy = resultTypes.Contains(ResultType.NodalVelocity) ? ApplyFactors(record.Vy, factorsLengthTime) : null;
      record.Vz = resultTypes.Contains(ResultType.NodalVelocity) ? ApplyFactors(record.Vz, factorsLengthTime) : null;
      record.Vxx = resultTypes.Contains(ResultType.NodalVelocity) ? ApplyFactors(record.Vxx, factorsLengthTime) : null;
      record.Vyy = resultTypes.Contains(ResultType.NodalVelocity) ? ApplyFactors(record.Vyy, factorsLengthTime) : null;
      record.Vzz = resultTypes.Contains(ResultType.NodalVelocity) ? ApplyFactors(record.Vzz, factorsLengthTime) : null;

      record.Ax = resultTypes.Contains(ResultType.NodalAcceleration) ? ApplyFactors(record.Ax, factorsAccel) : null;
      record.Ay = resultTypes.Contains(ResultType.NodalAcceleration) ? ApplyFactors(record.Ay, factorsAccel) : null;
      record.Az = resultTypes.Contains(ResultType.NodalAcceleration) ? ApplyFactors(record.Az, factorsAccel) : null;
      record.Axx = resultTypes.Contains(ResultType.NodalAcceleration) ? ApplyFactors(record.Axx, factorsAccel) : null;
      record.Ayy = resultTypes.Contains(ResultType.NodalAcceleration) ? ApplyFactors(record.Ayy, factorsAccel) : null;
      record.Azz = resultTypes.Contains(ResultType.NodalAcceleration) ? ApplyFactors(record.Azz, factorsAccel) : null;

      record.Fx_Reac = resultTypes.Contains(ResultType.NodalReaction) ? ApplyFactors(record.Fx_Reac, factorsForce) : null;
      record.Fy_Reac = resultTypes.Contains(ResultType.NodalReaction) ? ApplyFactors(record.Fy_Reac, factorsForce) : null;
      record.Fz_Reac = resultTypes.Contains(ResultType.NodalReaction) ? ApplyFactors(record.Fz_Reac, factorsForce) : null;
      record.Mxx_Reac = resultTypes.Contains(ResultType.NodalReaction) ? ApplyFactors(record.Mxx_Reac, factorsForceLength) : null;
      record.Myy_Reac = resultTypes.Contains(ResultType.NodalReaction) ? ApplyFactors(record.Myy_Reac, factorsForceLength) : null;
      record.Mzz_Reac = resultTypes.Contains(ResultType.NodalReaction) ? ApplyFactors(record.Mzz_Reac, factorsForceLength) : null;

      record.Fx_Cons = resultTypes.Contains(ResultType.ConstraintForces) ? ApplyFactors(record.Fx_Cons, factorsForce) : null;
      record.Fy_Cons = resultTypes.Contains(ResultType.ConstraintForces) ? ApplyFactors(record.Fy_Cons, factorsForce) : null;
      record.Fz_Cons = resultTypes.Contains(ResultType.ConstraintForces) ? ApplyFactors(record.Fz_Cons, factorsForce) : null;
      record.Mxx_Cons = resultTypes.Contains(ResultType.ConstraintForces) ? ApplyFactors(record.Mxx_Cons, factorsForceLength) : null;
      record.Myy_Cons = resultTypes.Contains(ResultType.ConstraintForces) ? ApplyFactors(record.Myy_Cons, factorsForceLength) : null;
      record.Mzz_Cons = resultTypes.Contains(ResultType.ConstraintForces) ? ApplyFactors(record.Mzz_Cons, factorsForceLength) : null;

      return true;
    }
  }
}
