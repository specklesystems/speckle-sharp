using System;
using Objects.Structural.Geometry;
using Objects.Geometry;
using Objects.Structural.Analysis;
using System.Collections.Generic;
using Speckle.Core.Models;

using ETABSv1;
using System.Linq;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
    public object PointToNative(Node speckleStructNode)
    {
      if (GetAllPointNames(Model).Contains(speckleStructNode.name))
      {
        return null;
      }
      var point = speckleStructNode.basePoint;
      string name = "";
      Model.PointObj.AddCartesian(point.x, point.y, point.z, ref name);
      if (speckleStructNode.name != null)
      {
        Model.PointObj.ChangeName(name, speckleStructNode.name);
      }

      return speckleStructNode.name;
    }
    public Node PointToSpeckle(string name)
    {
      var speckleStructNode = new Node();
      double x, y, z;
      x = y = z = 0;
      int v = Model.PointObj.GetCoordCartesian(name, ref x, ref y, ref z);
      speckleStructNode.basePoint = new Point();
      speckleStructNode.basePoint.x = x;
      speckleStructNode.basePoint.y = y;
      speckleStructNode.basePoint.z = z;
      speckleStructNode.name = name;
      speckleStructNode.units = ModelUnits();
      speckleStructNode.basePoint.units = ModelUnits();

      bool[] restraints = null;
      v = Model.PointObj.GetRestraint(name, ref restraints);

      speckleStructNode.restraint = RestraintToSpeckle(restraints);

      SpeckleModel.restraints.Add(speckleStructNode.restraint);

      var GUID = "";
      Model.PointObj.GetGUID(name, ref GUID);
      speckleStructNode.applicationId = GUID;
      List<Base> nodes = SpeckleModel.nodes;
      List<string> application_Id = nodes.Select(o => o.applicationId).ToList();
      if (!application_Id.Contains(speckleStructNode.applicationId))
      {
        SpeckleModel.nodes.Add(speckleStructNode);
      }
      //SpeckleModel.nodes.Add(speckleStructNode);

      return speckleStructNode;
    }

  }
}
