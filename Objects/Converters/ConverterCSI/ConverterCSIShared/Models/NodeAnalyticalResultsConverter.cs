#nullable enable
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Loading;
using Objects.Structural.Results;
using Speckle.Core.Models;

namespace ConverterCSIShared.Models;

internal class NodeAnalyticalResultsConverter
{
  private readonly cSapModel sapModel;
  private readonly Dictionary<string, Base> loadCombinationsAndCases;
  private readonly bool sendDisplacements;
  private readonly bool sendForces;
  private readonly bool sendVelocity;
  private readonly bool sendAcceleration;

  public NodeAnalyticalResultsConverter(
    cSapModel sapModel,
    IEnumerable<LoadCombination> loadCombinations,
    IEnumerable<LoadCase> loadCases,
    bool sendDisplacements,
    bool sendForces,
    bool sendVelocity,
    bool sendAcceleration
  )
  {
    this.sapModel = sapModel;
    this.sapModel = sapModel;

    this.loadCombinationsAndCases = new();
    foreach (var combo in loadCombinations)
    {
      this.loadCombinationsAndCases.Add(combo.name, combo);
    }

    foreach (var loadCase in loadCases)
    {
      this.loadCombinationsAndCases.Add(loadCase.name, loadCase);
    }

    this.sendDisplacements = sendDisplacements;
    this.sendForces = sendForces;
    this.sendVelocity = sendVelocity;
    this.sendAcceleration = sendAcceleration;
  }

  public AnalyticalResults AnalyticalResultsToSpeckle(string nodeName)
  {
    return new() { resultsByLoadCombination = GetAnalysisResultsForNode(nodeName).Cast<Result>().ToList() };
  }

  private IEnumerable<ResultSetNode> GetAnalysisResultsForNode(string nodeName)
  {
    int numberResults = 0;
    string[] loadCases = null;

    bool displacementSuccess = AnalysisResultUtils.TryGetAPIResult(
      sapModel.Results.JointDispl,
      nodeName,
      out int numberNodeResults,
      out _,
      out _,
      out string[] nodeLoadCases,
      out _,
      out _,
      out double[] U1,
      out double[] U2,
      out double[] U3,
      out double[] R1,
      out double[] R2,
      out double[] R3,
      sendDisplacements
    );

    if (displacementSuccess)
    {
      numberResults = numberNodeResults;
      loadCases = nodeLoadCases;
    }

    bool forceSuccess = AnalysisResultUtils.TryGetAPIResult(
      sapModel.Results.JointReact,
      nodeName,
      out int numberForceResults,
      out _,
      out _,
      out string[] forceLoadCases,
      out _,
      out _,
      out double[] F1,
      out double[] F2,
      out double[] F3,
      out double[] M1,
      out double[] M2,
      out double[] M3,
      sendForces
    );

    if (forceSuccess)
    {
      numberResults = numberForceResults;
      loadCases = forceLoadCases;
    }

    bool velocitySuccess = AnalysisResultUtils.TryGetAPIResult(
      sapModel.Results.JointVelAbs,
      nodeName,
      out int numberVelocityResults,
      out _,
      out _,
      out string[] velocityLoadCases,
      out _,
      out _,
      out double[] U1Vel,
      out double[] U2Vel,
      out double[] U3Vel,
      out double[] R1Vel,
      out double[] R2Vel,
      out double[] R3Vel,
      sendVelocity
    );

    if (velocitySuccess)
    {
      numberResults = numberVelocityResults;
      loadCases = velocityLoadCases;
    }

    bool accelerationSuccess = AnalysisResultUtils.TryGetAPIResult(
      sapModel.Results.JointAccAbs,
      nodeName,
      out int numberAccelerationResults,
      out _,
      out _,
      out string[] accelerationLoadCases,
      out _,
      out _,
      out double[] U1Acc,
      out double[] U2Acc,
      out double[] U3Acc,
      out double[] R1Acc,
      out double[] R2Acc,
      out double[] R3Acc,
      sendAcceleration
    );

    if (accelerationSuccess)
    {
      numberResults = numberAccelerationResults;
      loadCases = accelerationLoadCases;
    }

    Dictionary<string, ResultSetNode> resultSets = new();
    for (int index = 0; index < numberResults; index++)
    {
      ResultNode speckleResultNode = new();

      if (forceSuccess)
      {
        speckleResultNode.reactionX = (float)F1[index];
        speckleResultNode.reactionY = (float)F2[index];
        speckleResultNode.reactionZ = (float)F3[index];
        speckleResultNode.reactionXX = (float)M1[index];
        speckleResultNode.reactionYY = (float)M2[index];
        speckleResultNode.reactionZZ = (float)M3[index];
      }

      if (displacementSuccess)
      {
        speckleResultNode.dispX = (float)U1[index];
        speckleResultNode.dispY = (float)U2[index];
        speckleResultNode.dispZ = (float)U3[index];
        speckleResultNode.rotXX = (float)R1[index];
        speckleResultNode.rotYY = (float)R2[index];
        speckleResultNode.rotZZ = (float)R3[index];
      }

      if (velocitySuccess)
      {
        speckleResultNode.velX = (float)U1Vel[index];
        speckleResultNode.velY = (float)U2Vel[index];
        speckleResultNode.velZ = (float)U3Vel[index];
        speckleResultNode.velXX = (float)R1Vel[index];
        speckleResultNode.velYY = (float)R2Vel[index];
        speckleResultNode.velZZ = (float)R3Vel[index];
      }

      if (accelerationSuccess)
      {
        speckleResultNode.accX = (float)U1Acc[index];
        speckleResultNode.accY = (float)U2Acc[index];
        speckleResultNode.accZ = (float)U3Acc[index];
        speckleResultNode.accXX = (float)R1Acc[index];
        speckleResultNode.accYY = (float)R2Acc[index];
        speckleResultNode.accZZ = (float)R3Acc[index];
      }

      GetOrCreateResult(resultSets, loadCases[index]).resultsNode.Add(speckleResultNode);
    }

    return resultSets.Values;
  }

  private ResultSetNode GetOrCreateResult(Dictionary<string, ResultSetNode> dict, string loadCaseName)
  {
    if (!dict.TryGetValue(loadCaseName, out ResultSetNode comboResults))
    {
      Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
      comboResults = new ResultSetNode(new()) { resultCase = loadCaseOrCombination };
      dict[loadCaseName] = comboResults;
    }
    return comboResults;
  }
}
