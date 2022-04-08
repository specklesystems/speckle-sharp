using System;
using Objects.Structural.Geometry;
using Objects.Geometry;
using Objects.Structural.Analysis;
using System.Collections.Generic;
using Objects.Structural.ETABS.Geometry;
using Objects.Structural.ETABS.Properties;
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
      if (speckleStructNode.restraint != null)
      {
        var restraint = RestraintToNative(speckleStructNode.restraint);
        Model.PointObj.SetRestraint(name, ref restraint);
      }


      if (speckleStructNode.name != null)
      {
        Model.PointObj.ChangeName(name, speckleStructNode.name);
      }
      else { Model.PointObj.ChangeName(name, speckleStructNode.id); }

      if (speckleStructNode is ETABSNode)
      {
        var ETABSnode = (ETABSNode)speckleStructNode;
        if (ETABSnode.ETABSSpringProperty != null) { Model.PointObj.SetSpringAssignment(ETABSnode.name, ETABSnode.ETABSSpringProperty.name); }
        if (ETABSnode.DiaphragmAssignment != null)
        {
          switch (ETABSnode.DiaphragmOption)
          {
            case DiaphragmOption.Disconnect:
              Model.PointObj.SetDiaphragm(ETABSnode.name, eDiaphragmOption.Disconnect, DiaphragmName: ETABSnode.DiaphragmAssignment);
              break;
            case DiaphragmOption.DefinedDiaphragm:
              Model.PointObj.SetDiaphragm(ETABSnode.name, eDiaphragmOption.DefinedDiaphragm, DiaphragmName: ETABSnode.DiaphragmAssignment);
              break;
            case DiaphragmOption.FromShellObject:
              Model.PointObj.SetDiaphragm(ETABSnode.name, eDiaphragmOption.FromShellObject, DiaphragmName: ETABSnode.DiaphragmAssignment);
              break;
          }
        }

      }

      return speckleStructNode.name;
    }
    public ETABSNode PointToSpeckle(string name)
    {
      var speckleStructNode = new ETABSNode();
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

      string SpringProp = null;
      Model.PointObj.GetSpringAssignment(name, ref SpringProp);
      if (SpringProp != null) { speckleStructNode.ETABSSpringProperty = SpringPropertyToSpeckle(SpringProp); }

      string diaphragmAssignment = null;
      eDiaphragmOption eDiaphragmOption = eDiaphragmOption.Disconnect;
      Model.PointObj.GetDiaphragm(name, ref eDiaphragmOption, ref diaphragmAssignment);
      if (diaphragmAssignment != null)
      {
        speckleStructNode.DiaphragmAssignment = diaphragmAssignment;
        switch (eDiaphragmOption)
        {
          case eDiaphragmOption.Disconnect:
            speckleStructNode.DiaphragmOption = DiaphragmOption.Disconnect;
            break;
          case eDiaphragmOption.FromShellObject:
            speckleStructNode.DiaphragmOption = DiaphragmOption.FromShellObject;
            break;
          case eDiaphragmOption.DefinedDiaphragm:
            speckleStructNode.DiaphragmOption = DiaphragmOption.DefinedDiaphragm;
            break;
        }
      }

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