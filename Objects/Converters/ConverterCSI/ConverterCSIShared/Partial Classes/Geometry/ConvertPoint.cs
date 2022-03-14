using System;
using Objects.Structural.Geometry;
using Objects.Geometry;
using Objects.Structural.Analysis;
using System.Collections.Generic;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;

using CSiAPIv1;
using System.Linq;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
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

      if (speckleStructNode is CSINode)
      {
        var CSInode = (CSINode)speckleStructNode;
        if (CSInode.CSISpringProperty != null) { Model.PointObj.SetSpringAssignment(CSInode.name, CSInode.CSISpringProperty.name); }
        if (CSInode.DiaphragmAssignment != null)
        {
          switch (CSInode.DiaphragmOption)
          {
            case DiaphragmOption.Disconnect:
              Model.PointObj.SetDiaphragm(CSInode.name, eDiaphragmOption.Disconnect, DiaphragmName: CSInode.DiaphragmAssignment);
              break;
            case DiaphragmOption.DefinedDiaphragm:
              Model.PointObj.SetDiaphragm(CSInode.name, eDiaphragmOption.DefinedDiaphragm, DiaphragmName: CSInode.DiaphragmAssignment);
              break;
            case DiaphragmOption.FromShellObject:
              Model.PointObj.SetDiaphragm(CSInode.name, eDiaphragmOption.FromShellObject, DiaphragmName: CSInode.DiaphragmAssignment);
              break;
          }
        }

      }

      return speckleStructNode.name;
    }
    public CSINode PointToSpeckle(string name)
    {
      var speckleStructNode = new CSINode();
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
      if (SpringProp != null) { speckleStructNode.CSISpringProperty = SpringPropertyToSpeckle(SpringProp); }

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