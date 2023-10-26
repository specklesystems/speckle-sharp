using Objects.Structural.Loading;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using System.Linq;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    readonly Dictionary<string, LoadNode> _loadStoringNode = new();
    readonly Dictionary<string, List<Node>> _nodeStoring = new();
    int counterPoint;

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

      int s = Model.PointObj.GetLoadForce(
        name,
        ref numberItems,
        ref pointName,
        ref loadPat,
        ref lcStep,
        ref csys,
        ref F1,
        ref F2,
        ref F3,
        ref M1,
        ref M2,
        ref M3
      );
      if (s == 0)
      {
        foreach (int index in Enumerable.Range(0, numberItems))
        {
          LoadDirection direction;
          string loadId;
          double value;
          if (F1[index] != 0)
          {
            direction = LoadDirection.X;
            loadId = string.Concat(loadPat[index], F1[index], "F1");
            value = F1[index];
          }
          else if (F2[index] != 0)
          {
            direction = LoadDirection.Y;
            loadId = string.Concat(loadPat[index], F2[index], "F2");
            value = F2[index];
          }
          else if (F3[index] != 0)
          {
            direction = LoadDirection.Z;
            loadId = string.Concat(loadPat[index], F3[index], "F3");
            value = F3[index];
          }
          else if (M1[index] != 0)
          {
            direction = LoadDirection.XX;
            loadId = string.Concat(loadPat[index], M1[index], "M1");
            value = M1[index];
          }
          else if (M2[index] != 0)
          {
            direction = LoadDirection.YY;
            loadId = string.Concat(loadPat[index], M2[index], "M2");
            value = M2[index];
          }
          else if (M3[index] != 0)
          {
            direction = LoadDirection.ZZ;
            loadId = string.Concat(loadPat[index], M3[index], "M3");
            value = M3[index];
          }
          else
          {
            continue;
          }

          var speckleLoadNode = new LoadNode();
          speckleLoadNode.direction = direction;
          GenerateIdAndElements(loadId, loadPat[index], pointName[index], value, speckleLoadNode);
          //speckleLoadFace.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
        }
        counterPoint += 1;

        if (counterPoint == pointNumber)
        {
          foreach (var entry in _nodeStoring.Keys)
          {
            _nodeStoring.TryGetValue(entry, out var listNode);
            _loadStoringNode.TryGetValue(entry, out var loadStoringNode);
            loadStoringNode.nodes = listNode;
            SpeckleModel.loads.Add(loadStoringNode);
          }
        }
      }

      var speckleBase = new Base();
      return speckleBase;
    }

    void GenerateIdAndElements(string loadId, string loadPat, string nodeName, double value, LoadNode speckleLoadNode)
    {
      speckleLoadNode.value = value;
      Node speckleNode = PointToSpeckle(nodeName);
      speckleLoadNode.loadCase = LoadPatternCaseToSpeckle(loadPat);
      _nodeStoring.TryGetValue(loadId, out var nodeList);
      nodeList ??= new List<Node>();
      nodeList.Add(speckleNode);
      _nodeStoring[loadId] = nodeList;
      _loadStoringNode[loadId] = speckleLoadNode;
    }
  }
}
