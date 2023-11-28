using System;
using Objects.Structural.Geometry;
using Objects.Geometry;
using System.Collections.Generic;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;

using CSiAPIv1;
using System.Linq;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public string UpdatePoint(string name, Node speckleNode, Point basePoint = null)
  {
    basePoint = basePoint ?? speckleNode.basePoint;
    if (basePoint == null)
    {
      return name;
    }

    CreatePoint(basePoint, out string newName);
    name = newName;

    UpdatePointProperties(speckleNode, ref name);
    return name;
  }

  public void UpdatePointProperties(Node speckleStructNode, ref string name)
  {
    if (speckleStructNode == null)
    {
      return;
    }

    if (speckleStructNode.restraint != null)
    {
      var restraint = RestraintToNative(speckleStructNode.restraint);
      Model.PointObj.SetRestraint(name, ref restraint);
    }

    if (speckleStructNode.name != null)
    {
      Model.PointObj.ChangeName(name, speckleStructNode.name);
      name = speckleStructNode.name;
    }

    if (!(speckleStructNode is CSINode csiNode))
    {
      return;
    }

    if (csiNode.CSISpringProperty != null)
    {
      Model.PointObj.SetSpringAssignment(csiNode.name, csiNode.CSISpringProperty.name);
    }

    if (csiNode.DiaphragmAssignment != null)
    {
      switch (csiNode.DiaphragmOption)
      {
        case DiaphragmOption.Disconnect:
          Model.PointObj.SetDiaphragm(
            csiNode.name,
            eDiaphragmOption.Disconnect,
            DiaphragmName: csiNode.DiaphragmAssignment
          );
          break;
        case DiaphragmOption.DefinedDiaphragm:
          Model.PointObj.SetDiaphragm(
            csiNode.name,
            eDiaphragmOption.DefinedDiaphragm,
            DiaphragmName: csiNode.DiaphragmAssignment
          );
          break;
        case DiaphragmOption.FromShellObject:
          Model.PointObj.SetDiaphragm(
            csiNode.name,
            eDiaphragmOption.FromShellObject,
            DiaphragmName: csiNode.DiaphragmAssignment
          );
          break;
      }
    }
  }

  public string PointToNative(Node speckleStructNode, IList<string>? notes)
  {
    if (GetAllPointNames(Model).Contains(speckleStructNode.name))
    {
      notes?.Add($"node with name {speckleStructNode.name} already exists");
      return speckleStructNode.name;
    }

    var point = speckleStructNode.basePoint;
    if (point == null)
    {
      throw new ArgumentException($"Node does not have a valid location, {nameof(Node.basePoint)} was null");
    }

    var success = CreatePoint(point, out string name);
    UpdatePointProperties(speckleStructNode, ref name);

    if (success != 0)
    {
      throw new ConversionException("Failed create point");
    }

    return speckleStructNode.name;
  }

  public int CreatePoint(Point point, out string name)
  {
    var scaleFactor = Units.GetConversionFactor(point.units, ModelUnits());

    name = null;
    var success = Model.PointObj.AddCartesian(
      point.x * scaleFactor,
      point.y * scaleFactor,
      point.z * scaleFactor,
      ref name
    );
    return success;
  }

  public CSINode PointToSpeckle(string name)
  {
    var speckleStructNode = new CSINode();
    double x,
      y,
      z;
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

    SpeckleModel?.restraints.Add(speckleStructNode.restraint);

    string SpringProp = null;
    Model.PointObj.GetSpringAssignment(name, ref SpringProp);
    if (SpringProp != null)
    {
      speckleStructNode.CSISpringProperty = SpringPropertyToSpeckle(SpringProp);
    }

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

    speckleStructNode.AnalysisResults = resultsConverter?.NodeAnalyticalResultsConverter?.AnalyticalResultsToSpeckle(
      speckleStructNode.name
    );

    var GUID = "";
    Model.PointObj.GetGUID(name, ref GUID);
    speckleStructNode.applicationId = GUID;
    List<Base> nodes = SpeckleModel == null ? new List<Base>() : SpeckleModel.nodes;
    List<string> application_Id = nodes.Select(o => o.applicationId).ToList();
    if (!application_Id.Contains(speckleStructNode.applicationId))
    {
      SpeckleModel?.nodes.Add(speckleStructNode);
    }
    //SpeckleModel.nodes.Add(speckleStructNode);

    return speckleStructNode;
  }
}
