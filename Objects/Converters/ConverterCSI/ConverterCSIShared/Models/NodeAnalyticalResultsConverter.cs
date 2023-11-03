using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Results;
using Speckle.Core.Models;

namespace ConverterCSIShared.Models
{
  internal class NodeAnalyticalResultsConverter
  {
    private readonly Model speckleModel;
    private readonly cSapModel sapModel;
    private readonly Dictionary<string, Base> loadCombinationsAndCases;
    private readonly bool sendVelocityAccelerationData;
    public NodeAnalyticalResultsConverter(
      Model speckleModel,
      cSapModel sapModel,
      IEnumerable<LoadCombination> loadCombinations,
      IEnumerable<LoadCase> loadCases,
      bool sendVelocityAccelerationData)
    {
      this.speckleModel = speckleModel;
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

      this.sendVelocityAccelerationData = sendVelocityAccelerationData;
    }

    public void AnalyticalResultsToSpeckle()
    {
      foreach (Base element in speckleModel.nodes)
      {
        if (element is not CSINode node)
        {
          continue;
        }

        AnalyticalResults results = new()
        {
          resultsByLoadCombination = GetAnalysisResultsForNode(node).Cast<Result>().ToList()
        };
        node.AnalysisResults = results;
      }
    }

    public IEnumerable<ResultSetNode> GetAnalysisResultsForNode(Node node)
    {
      int numberResults = 0;
      string[] obj = null;
      string[] elm = null;
      string[] loadCases = null;
      string[] stepType = null;
      double[] stepNum = null;
      double[] F1 = null;
      double[] F2 = null;
      double[] F3 = null;
      double[] M1 = null;
      double[] M2 = null;
      double[] M3 = null;

      double[] U1 = null;
      double[] U2 = null;
      double[] U3 = null;
      double[] R1 = null;
      double[] R2 = null;
      double[] R3 = null;

      double[] U1Vel = null;
      double[] U2Vel = null;
      double[] U3Vel = null;
      double[] R1Vel = null;
      double[] R2Vel = null;
      double[] R3Vel = null;

      double[] U1Acc = null;
      double[] U2Acc = null;
      double[] U3Acc = null;
      double[] R1Acc = null;
      double[] R2Acc = null;
      double[] R3Acc = null;

      int numberGroundResults = 0;
      string[] loadCasesGround = null;

      sapModel.Results.JointReact(
        node.name,
        eItemTypeElm.Element,
        ref numberResults,
        ref obj,
        ref elm,
        ref loadCases,
        ref stepType,
        ref stepNum,
        ref F1,
        ref F2,
        ref F3,
        ref M1,
        ref M2,
        ref M3
      );
      sapModel.Results.JointDispl(
        node.name,
        eItemTypeElm.Element,
        ref numberResults,
        ref obj,
        ref elm,
        ref loadCases,
        ref stepType,
        ref stepNum,
        ref U1,
        ref U2,
        ref U3,
        ref R1,
        ref R2,
        ref R3
      );

      Dictionary<string, ResultSetNode> resultSets = new();
      for (int index = 0; index < numberResults; index++)
      {
        var speckleResultNode = new ResultNode
        {
          reactionX = (float)F1[index],
          reactionY = (float)F2[index],
          reactionZ = (float)F3[index],
          reactionXX = (float)M1[index],
          reactionYY = (float)M2[index],
          reactionZZ = (float)M3[index],

          rotXX = (float)R1[index],
          rotYY = (float)R2[index],
          rotZZ = (float)R3[index],
          dispX = (float)U1[index],
          dispY = (float)U2[index],
          dispZ = (float)U3[index]
        };
        if (sendVelocityAccelerationData)
        {
          AddVelocityAccelerationData(
            node,
            obj,
            elm,
            stepType,
            stepNum,
            U1Vel,
            U2Vel,
            U3Vel,
            R1Vel,
            R2Vel,
            R3Vel,
            U1Acc,
            U2Acc,
            U3Acc,
            R1Acc,
            R2Acc,
            R3Acc,
            numberGroundResults,
            loadCasesGround,
            speckleResultNode);
        }

        GetOrCreateResult(resultSets, loadCases[index]).resultsNode.Add(speckleResultNode);
      }


      return resultSets.Values;
    }

    private void AddVelocityAccelerationData(
      Node node,
      string[] obj,
      string[] elm,
      string[] stepType,
      double[] stepNum,
      double[] U1Vel,
      double[] U2Vel,
      double[] U3Vel,
      double[] R1Vel,
      double[] R2Vel,
      double[] R3Vel,
      double[] U1Acc,
      double[] U2Acc,
      double[] U3Acc,
      double[] R1Acc,
      double[] R2Acc,
      double[] R3Acc,
      int numberGroundResults,
      string[] loadCasesGround,
      ResultNode speckleResultNode)
    {
      var s = sapModel.Results.JointVelAbs(
        node.name,
        eItemTypeElm.Element,
        ref numberGroundResults,
        ref obj,
        ref elm,
        ref loadCasesGround,
        ref stepType,
        ref stepNum,
        ref U1Vel,
        ref U2Vel,
        ref U3Vel,
        ref R1Vel,
        ref R2Vel,
        ref R3Vel
      );
      var z = sapModel.Results.JointAccAbs(
        node.name,
        eItemTypeElm.Element,
        ref numberGroundResults,
        ref obj,
        ref elm,
        ref loadCasesGround,
        ref stepType,
        ref stepNum,
        ref U1Acc,
        ref U2Acc,
        ref U3Acc,
        ref R1Acc,
        ref R2Acc,
        ref R3Acc
      );
      if (s == 0 && z == 0)
      {
        foreach (int index in Enumerable.Range(0, numberGroundResults))
        {
          speckleResultNode.velX = (float)U1Vel[index];
          speckleResultNode.velY = (float)U2Vel[index];
          speckleResultNode.velZ = (float)U3Vel[index];
          speckleResultNode.velXX = (float)R1Vel[index];
          speckleResultNode.velYY = (float)R2Vel[index];
          speckleResultNode.velZZ = (float)R3Vel[index];

          speckleResultNode.accX = (float)U1Acc[index];
          speckleResultNode.accY = (float)U2Acc[index];
          speckleResultNode.accZ = (float)U3Acc[index];
          speckleResultNode.accXX = (float)R1Acc[index];
          speckleResultNode.accYY = (float)R2Acc[index];
          speckleResultNode.accZZ = (float)R3Acc[index];
        }
      }
    }

    private ResultSetNode GetOrCreateResult(Dictionary<string, ResultSetNode> dict, string loadCaseName)
    {
      if (!dict.TryGetValue(loadCaseName, out ResultSetNode comboResults))
      {
        Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
        comboResults = new ResultSetNode(new())
        {
          resultCase = loadCaseOrCombination
        };
        dict[loadCaseName] = comboResults;
      }
      return comboResults;
    }
  }
}
