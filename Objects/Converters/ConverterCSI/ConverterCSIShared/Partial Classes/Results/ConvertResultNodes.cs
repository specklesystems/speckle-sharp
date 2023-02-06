using CSiAPIv1;
using Objects.Structural.Geometry;
using Objects.Structural.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public ResultSetNode AllResultSetNodesToSpeckle()
    {
      var speckleResultNodeSet = new ResultSetNode();
      speckleResultNodeSet.resultsNode = new List<ResultNode> { };
      List<string> ListPoints = GetAllPointNames(Model);
      foreach (string pointName in ListPoints)
      {
        ResultNodeToSpeckle(pointName, speckleResultNodeSet);
      }
      return speckleResultNodeSet;
    }

    public void ResultNodeToSpeckle(string pointName, ResultSetNode resultSetNode)
    {
      var node = SpeckleModel.nodes.Where(o => (string)o["name"] == pointName).FirstOrDefault() as Node;

      // if the node is null, then it was not part of the user's selection, so don't send the results
      if (node == null)
        return;

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

      Model.Results.JointReact(pointName, eItemTypeElm.Element, ref numberResults, ref obj, ref elm, ref loadCases, ref stepType, ref stepNum, ref F1, ref F2, ref F3, ref M1, ref M2, ref M3);
      Model.Results.JointDispl(pointName, eItemTypeElm.Element, ref numberResults, ref obj, ref elm, ref loadCases, ref stepType, ref stepNum, ref U1, ref U2, ref U3, ref R1, ref R2, ref R3);

      foreach (int index in Enumerable.Range(0, numberResults))
      {
        var speckleResultNode = new ResultNode();
        speckleResultNode.node = node;
        speckleResultNode.reactionX = (float)F1[index];
        speckleResultNode.reactionY = (float)F2[index];
        speckleResultNode.reactionZ = (float)F3[index];
        speckleResultNode.reactionXX = (float)M1[index];
        speckleResultNode.reactionYY = (float)M2[index];
        speckleResultNode.reactionZZ = (float)M3[index];

        speckleResultNode.rotXX = (float)R1[index];
        speckleResultNode.rotYY = (float)R2[index];
        speckleResultNode.rotZZ = (float)R3[index];
        speckleResultNode.dispX = (float)U1[index];
        speckleResultNode.dispY = (float)U2[index];
        speckleResultNode.dispZ = (float)U3[index];

        speckleResultNode.resultCase = LoadPatternCaseToSpeckle(loadCases[index]);
        resultSetNode.resultsNode.Add(speckleResultNode);

      }


      var s = Model.Results.JointVelAbs(pointName, eItemTypeElm.Element, ref numberGroundResults, ref obj, ref elm, ref loadCasesGround, ref stepType, ref stepNum, ref U1Vel, ref U2Vel, ref U3Vel, ref R1Vel, ref R2Vel, ref R3Vel);
      var z = Model.Results.JointAccAbs(pointName, eItemTypeElm.Element, ref numberGroundResults, ref obj, ref elm, ref loadCasesGround, ref stepType, ref stepNum, ref U1Acc, ref U2Acc, ref U3Acc, ref R1Acc, ref R2Acc, ref R3Acc);
      if (s == 0 && z == 0)
      {
        foreach (int index in Enumerable.Range(0, numberGroundResults))
        {
          var speckleResultNode = new ResultNode();
          speckleResultNode.node = node;
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

          speckleResultNode.resultCase = LoadPatternCaseToSpeckle(loadCases[index]);
          resultSetNode.resultsNode.Add(speckleResultNode);

        }
      }

      return;
    }
  }
}
