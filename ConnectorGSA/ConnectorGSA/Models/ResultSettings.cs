using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;

namespace ConnectorGSA.Models
{
  public class ResultSettings
  {
    public List<ResultSettingItem> ResultSettingItems { get; set; }

    public ResultSettings()
    {

      //ResultSettingItems = Instance.GsaModel.Proxy.ResultTypeStrings.Keys.Select(k => new ResultSettingItem(GsaProxy.ResultTypeStrings[k], k, true)).ToList();

      ResultSettingItems = new List<ResultSettingItem>()
      {
        new ResultSettingItem("Nodal Displacements", ResultType.NodalDisplacements, true),
        new ResultSettingItem("Nodal Velocity", ResultType.NodalVelocity, false),
        new ResultSettingItem("Nodal Acceleration", ResultType.NodalAcceleration, false),
        new ResultSettingItem("Nodal Reaction", ResultType.NodalReaction, true),
        new ResultSettingItem("Constraint Forces", ResultType.ConstraintForces, true),
        new ResultSettingItem("1D Element Displacement", ResultType.Element1dDisplacement, false),
        new ResultSettingItem("1D Element Force", ResultType.Element1dForce, true),
        new ResultSettingItem("2D Element Displacement", ResultType.Element2dDisplacement, true),
        new ResultSettingItem("2D Element Projected Moment", ResultType.Element2dProjectedMoment,true),
        new ResultSettingItem("2D Element Projected Force", ResultType.Element2dProjectedForce, false),
        new ResultSettingItem("2D Element Projected Stress - Bottom", ResultType.Element2dProjectedStressBottom, false),
        new ResultSettingItem("2D Element Projected Stress - Middle", ResultType.Element2dProjectedStressMiddle, false),
        new ResultSettingItem("2D Element Projected Stress - Top", ResultType.Element2dProjectedStressTop, false),
        new ResultSettingItem("Assembly Forces and Moments", ResultType.AssemblyForcesAndMoments, true)
      };
    }
  }
}