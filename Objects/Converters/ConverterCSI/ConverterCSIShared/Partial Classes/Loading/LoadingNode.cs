using CSiAPIv1;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using System.Linq;
using System.Text;
using Speckle.Core.Models;
using Objects.Structural.CSI.Loading;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    Dictionary<string, LoadNode> LoadStoringNode = new Dictionary<string, LoadNode>();
    Dictionary<string, List<Node>> NodeStoring = new Dictionary<string, List<Node>>();
    int counterPoint = 0;
    //need to figure out how to recombine forces into val6
    //void LoadNodeToNative(LoadNode loadNode)
    //{
    //    foreach(var node in loadNode.nodes)
    //    {
    //        Model.PointObj.SetLoadForce(node.name,loadNode.loadCase,load)
    //    }
    //}
    Base LoadNodeToSpeckle(string name, int pointNumber)
    {
      double[] F1 = null;
      double[] F2 = null;
      double[] F3 = null;
      double[] M1 = null;
      double[] M2 = null;
      double[] M3 = null;
      string[] csys = null;
      int[] lcStep = null;
      string[] pointName = null;
      string[] loadPat = null;
      int numberItems = 0;

      int s = Model.PointObj.GetLoadForce(name, ref numberItems, ref pointName, ref loadPat, ref lcStep, ref csys, ref F1, ref F2, ref F3, ref M1, ref M2, ref M3);
      if (s == 0)
      {
        foreach (int index in Enumerable.Range(0, numberItems))
        {
          if (F1[index] != 0)
          {
            var speckleLoadNode = new LoadNode();
            speckleLoadNode.direction = LoadDirection.X;
            var loadID = string.Concat(loadPat[index], F1[index], "F1");
            generateIDAndElements(loadID, loadPat[index], pointName[index], F1[index], speckleLoadNode);
          }
          else if (F2[index] != 0)
          {
            var speckleLoadNode = new LoadNode();
            speckleLoadNode.direction = LoadDirection.Y;
            var loadID = string.Concat(loadPat[index], F2[index], "F2");
            generateIDAndElements(loadID, loadPat[index], pointName[index], F2[index], speckleLoadNode);
          }
          else if (F3[index] != 0)
          {
            var speckleLoadNode = new LoadNode();
            speckleLoadNode.direction = LoadDirection.Z;
            var loadID = string.Concat(loadPat[index], F3[index], "F3");
            generateIDAndElements(loadID, loadPat[index], pointName[index], F3[index], speckleLoadNode);
          }
          else if (M1[index] != 0)
          {
            var speckleLoadNode = new LoadNode();
            speckleLoadNode.direction = LoadDirection.XX;
            var loadID = string.Concat(loadPat[index], M1[index], "M1");
            generateIDAndElements(loadID, loadPat[index], pointName[index], M1[index], speckleLoadNode);
          }
          else if (M2[index] != 0)
          {
            var speckleLoadNode = new LoadNode();
            speckleLoadNode.direction = LoadDirection.YY;
            var loadID = string.Concat(loadPat[index], M2[index], "M2");
            generateIDAndElements(loadID, loadPat[index], pointName[index], M2[index], speckleLoadNode);
          }
          else if (M3[index] != 0)
          {
            var speckleLoadNode = new LoadNode();
            speckleLoadNode.direction = LoadDirection.ZZ;
            var loadID = string.Concat(loadPat[index], M3[index], "M3");
            generateIDAndElements(loadID, loadPat[index], pointName[index], M3[index], speckleLoadNode);
          }
          //speckleLoadFace.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);

        }
        counterPoint += 1;

        if (counterPoint == pointNumber)
        {
          foreach (var entry in NodeStoring.Keys)
          {
            NodeStoring.TryGetValue(entry, out var listNode);
            LoadStoringNode.TryGetValue(entry, out var loadStoringNode);
            loadStoringNode.nodes = listNode;
            SpeckleModel.loads.Add(loadStoringNode);

          }
        }
      }

      var speckleBase = new Base();
      return speckleBase;
    }

    void generateIDAndElements(string loadID, string loadPat, string nodeName, double value, LoadNode speckleLoadNode)
    {
      speckleLoadNode.value = value;
      Node speckleNode = PointToSpeckle(nodeName);
      speckleLoadNode.loadCase = LoadPatternCaseToSpeckle(loadPat);
      NodeStoring.TryGetValue(loadID, out var nodeList);
      if (nodeList == null) { nodeList = new List<Node> { }; }
      nodeList.Add(speckleNode);
      NodeStoring[loadID] = nodeList;
      LoadStoringNode[loadID] = speckleLoadNode;

    }
  }
}