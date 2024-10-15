using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Speckle.Core.Models;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using System.Linq;
using CSiAPIv1;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void UpdateFrame(Element1D element1D, string name, ApplicationObject appObj)
  {
    var end1node = element1D.end1Node?.basePoint ?? element1D.baseLine?.start;
    var end2node = element1D.end2Node?.basePoint ?? element1D.baseLine?.end;

    if (end1node == null || end2node == null)
    {
      throw new ArgumentException($"Frame {element1D.name} does not have valid endpoints {end1node},{end2node}");
    }

    UpdateFrameLocation(name, end1node, end2node, appObj);
    SetFrameElementProperties(element1D, name, appObj.Log);
  }

  public void UpdateFrameLocation(string name, Point p1, Point p2, ApplicationObject appObj)
  {
    string pt1 = "";
    string pt2 = "";
    Model.FrameObj.GetPoints(name, ref pt1, ref pt2);

    // unfortunately this isn't as easy as just changing the coords of the end points of the frame,
    // as those points may be shared by other frames. Need to check if there are other frames using
    // those points and then check the new location of the endpoints to see if there are existing points
    // that could be used.
    var pt1Updated = UpdatePoint(pt1, null, p1);
    var pt2Updated = UpdatePoint(pt2, null, p2);

    int success = 0;
    if (pt1Updated != pt1 || pt2Updated != pt2)
    {
      success = Model.EditFrame.ChangeConnectivity(name, pt1Updated, pt2Updated);

      int numItems = 0;
      int[] objTypes = null;
      string[] objNames = null;
      int[] pointNums = null;
      Model.PointObj.GetConnectivity(pt1, ref numItems, ref objTypes, ref objNames, ref pointNums);
      if (numItems == 0)
      {
        Model.PointObj.DeleteSpecialPoint(pt1);
      }

      Model.PointObj.GetConnectivity(pt2, ref numItems, ref objTypes, ref objNames, ref pointNums);
      if (numItems == 0)
      {
        Model.PointObj.DeleteSpecialPoint(pt2);
      }
    }

    if (success != 0)
    {
      throw new ConversionException("Failed to change frame connectivity");
    }

    string guid = null;
    Model.FrameObj.GetGUID(name, ref guid);
    appObj.Update(status: ApplicationObject.State.Updated, createdId: guid, convertedItem: $"Frame{Delimiter}{name}");
  }

  public void FrameToNative(Element1D element1D, ApplicationObject appObj)
  {
    if (element1D.type == ElementType1D.Link)
    {
      string createdName = LinkToNative((CSIElement1D)element1D, appObj.Log);
      appObj.Update(status: ApplicationObject.State.Created, createdId: createdName);
      return;
    }

    if (ElementExistsWithApplicationId(element1D.applicationId, out string name))
    {
      UpdateFrame(element1D, name, appObj);
      return;
    }

    Line baseline = element1D.baseLine;
    string[] properties = Array.Empty<string>();
    int number = 0;
    int success = Model.PropFrame.GetNameList(ref number, ref properties);
    if (success != 0)
    {
      appObj.Update(logItem: "Failed to retrieve frame section properties");
    }

    if (!properties.Contains(element1D.property.name))
    {
      TryConvertProperty1DToNative(element1D.property, appObj.Log);
      Model.PropFrame.GetNameList(ref number, ref properties);
    }

    Point end1node;
    Point end2node;
    if (baseline != null)
    {
      end1node = baseline.start;
      end2node = baseline.end;
    }
    else
    {
      end1node = element1D.end1Node.basePoint;
      end2node = element1D.end2Node.basePoint;
    }

    CreateFrame(end1node, end2node, out var newFrame, out _, appObj);
    SetFrameElementProperties(element1D, newFrame, appObj.Log);
  }

  public void CreateFrame(
    Point p0,
    Point p1,
    out string newFrame,
    out string guid,
    ApplicationObject appObj,
    string type = "Default",
    string nameOverride = null
  )
  {
    newFrame = string.Empty;
    guid = string.Empty;

    int success = Model.FrameObj.AddByCoord(
      ScaleToNative(p0.x, p0.units),
      ScaleToNative(p0.y, p0.units),
      ScaleToNative(p0.z, p0.units),
      ScaleToNative(p1.x, p1.units),
      ScaleToNative(p1.y, p1.units),
      ScaleToNative(p1.z, p1.units),
      ref newFrame,
      type
    );

    Model.FrameObj.GetGUID(newFrame, ref guid);

    if (!string.IsNullOrEmpty(nameOverride) && !GetAllFrameNames(Model).Contains(nameOverride))
    {
      Model.FrameObj.ChangeName(newFrame, nameOverride);
      newFrame = nameOverride;
    }

    if (success != 0)
    {
      throw new ConversionException("Failed to add new frame object at the specified coordinates");
    }

    appObj.Update(
      status: ApplicationObject.State.Created,
      createdId: guid,
      convertedItem: $"Frame{Delimiter}{newFrame}"
    );
  }

  public CSIElement1D FrameToSpeckle(string name)
  {
    string units = ModelUnits();

    string pointI,
      pointJ;
    pointI = pointJ = null;
    _ = Model.FrameObj.GetPoints(name, ref pointI, ref pointJ);
    var pointINode = PointToSpeckle(pointI);
    var pointJNode = PointToSpeckle(pointJ);
    Line speckleLine;
    if (units != null)
    {
      speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint, units);
    }
    else
    {
      speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint);
    }

    string property,
      SAuto;
    property = SAuto = null;
    Model.FrameObj.GetSection(name, ref property, ref SAuto);
    var speckleProperty = Property1DToSpeckle(property);

    eFrameDesignOrientation frameDesignOrientation = eFrameDesignOrientation.Null;
    Model.FrameObj.GetDesignOrientation(name, ref frameDesignOrientation);
    var elementType = FrameDesignOrientationToElement1dType(frameDesignOrientation);
    var speckleStructFrame = new CSIElement1D(speckleLine, speckleProperty, elementType)
    {
      name = name,
      end1Node = pointINode,
      end2Node = pointJNode
    };

    bool[] iRelease,
      jRelease;
    iRelease = jRelease = null;
    double[] startV,
      endV;
    startV = endV = null;
    Model.FrameObj.GetReleases(name, ref iRelease, ref jRelease, ref startV, ref endV);

    speckleStructFrame.end1Releases = RestraintToSpeckle(iRelease);
    speckleStructFrame.end2Releases = RestraintToSpeckle(jRelease);
    SpeckleModel.restraints.Add(speckleStructFrame.end1Releases);
    SpeckleModel.restraints.Add(speckleStructFrame.end2Releases);

    double localAxis = 0;
    bool advanced = false;
    Model.FrameObj.GetLocalAxes(name, ref localAxis, ref advanced);
    speckleStructFrame.orientationAngle = localAxis;

    double offSetEnd1 = 0;
    double offSetEnd2 = 0;
    double RZ = 0;
    bool autoOffSet = true;
    Model.FrameObj.GetEndLengthOffset(name, ref autoOffSet, ref offSetEnd1, ref offSetEnd2, ref RZ);
    //Offset needs to be oriented wrt to 1-axis
    Vector end1Offset = new(0, 0, offSetEnd1, ModelUnits());
    Vector end2Offset = new(0, 0, offSetEnd2, ModelUnits());
    speckleStructFrame.end1Offset = end1Offset;
    speckleStructFrame.end2Offset = end2Offset;

    string springLineName = null;
    Model.FrameObj.GetSpringAssignment(name, ref springLineName);
    if (springLineName != null)
    {
      speckleStructFrame.CSILinearSpring = LinearSpringToSpeckle(springLineName);
    }

    string pierAssignment = null;
    Model.FrameObj.GetPier(name, ref pierAssignment);
    if (pierAssignment != null)
    {
      speckleStructFrame.PierAssignment = pierAssignment;
    }

    string spandrelAssignment = null;
    Model.FrameObj.GetSpandrel(name, ref spandrelAssignment);
    if (spandrelAssignment != null)
    {
      speckleStructFrame.SpandrelAssignment = spandrelAssignment;
    }

    int designProcedure = 9;
    Model.FrameObj.GetDesignProcedure(name, ref designProcedure);
    if (designProcedure != 9)
    {
      switch (designProcedure)
      {
        case 0:
          speckleStructFrame.DesignProcedure = DesignProcedure.ProgramDetermined;
          break;
        case 1:
          speckleStructFrame.DesignProcedure = DesignProcedure.SteelFrameDesign;
          break;
        case 2:
          speckleStructFrame.DesignProcedure = DesignProcedure.ConcreteFrameDesign;
          break;
        case 3:
          speckleStructFrame.DesignProcedure = DesignProcedure.CompositeBeamDesign;
          break;
        case 4:
          speckleStructFrame.DesignProcedure = DesignProcedure.SteelJoistDesign;
          break;
        case 7:
          speckleStructFrame.DesignProcedure = DesignProcedure.NoDesign;
          break;
        case 13:
          speckleStructFrame.DesignProcedure = DesignProcedure.CompositeColumnDesign;
          break;
      }
    }
    double[] modifiers = Array.Empty<double>();
    int s = Model.FrameObj.GetModifiers(name, ref modifiers);
    if (s == 0)
    {
      speckleStructFrame.StiffnessModifiers = modifiers.ToList();
    }

    speckleStructFrame.AnalysisResults =
      resultsConverter?.Element1DAnalyticalResultConverter?.AnalyticalResultsToSpeckle(
        speckleStructFrame.name,
        speckleStructFrame.type
      );

    var GUID = "";
    Model.FrameObj.GetGUID(name, ref GUID);
    speckleStructFrame.applicationId = GUID;
    List<Base> elements = SpeckleModel.elements;
    List<string> application_Id = elements.Select(o => o.applicationId).ToList();
    if (!application_Id.Contains(speckleStructFrame.applicationId))
    {
      SpeckleModel.elements.Add(speckleStructFrame);
    }

    return speckleStructFrame;
  }

  private static ElementType1D FrameDesignOrientationToElement1dType(eFrameDesignOrientation frameDesignOrientation) =>
    frameDesignOrientation switch
    {
      eFrameDesignOrientation.Column => ElementType1D.Column,
      eFrameDesignOrientation.Beam => ElementType1D.Beam,
      eFrameDesignOrientation.Brace => ElementType1D.Brace,
      eFrameDesignOrientation.Null => ElementType1D.Null,
      eFrameDesignOrientation.Other => ElementType1D.Other,
      _ => throw new SpeckleException($"Unrecognized eFrameDesignOrientation value, {frameDesignOrientation}"),
    };

  public void SetFrameElementProperties(Element1D element1D, string newFrame, IList<string>? log)
  {
    bool[] end1Release = null;
    bool[] end2Release = null;
    double[] startV,
      endV;
    startV = null;
    endV = null;
    if (element1D.end1Releases != null && element1D.end2Releases != null)
    {
      end1Release = RestraintToNative(element1D.end1Releases);
      end2Release = RestraintToNative(element1D.end2Releases);
      startV = PartialRestraintToNative(element1D.end1Releases);
      endV = PartialRestraintToNative(element1D.end2Releases);
    }

    var propertyName = TryConvertProperty1DToNative(element1D.property, log);
    if (propertyName != null)
    {
      Model.FrameObj.SetSection(newFrame, propertyName);
    }

    if (element1D.orientationAngle != null)
    {
      Model.FrameObj.SetLocalAxes(newFrame, element1D.orientationAngle);
    }
    end1Release = end1Release.Select(b => !b).ToArray();
    end2Release = end2Release.Select(b => !b).ToArray();

    Model.FrameObj.SetReleases(newFrame, ref end1Release, ref end2Release, ref startV, ref endV);
    if (element1D is CSIElement1D)
    {
      var CSIelement1D = (CSIElement1D)element1D;
      if (CSIelement1D.SpandrelAssignment != null)
      {
        Model.FrameObj.SetSpandrel(newFrame, CSIelement1D.SpandrelAssignment);
      }
      if (CSIelement1D.PierAssignment != null)
      {
        Model.FrameObj.SetPier(newFrame, CSIelement1D.PierAssignment);
      }
      if (CSIelement1D.CSILinearSpring != null)
      {
        Model.FrameObj.SetSpringAssignment(newFrame, CSIelement1D.CSILinearSpring.name);
      }

      if (CSIelement1D.StiffnessModifiers != null)
      {
        var modifiers = CSIelement1D.StiffnessModifiers.ToArray();
        Model.FrameObj.SetModifiers(newFrame, ref modifiers);
      }
      if (CSIelement1D.property.material.name != null)
      {
        switch (CSIelement1D.DesignProcedure)
        {
          case DesignProcedure.ProgramDetermined:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 0);
            break;
          case DesignProcedure.CompositeBeamDesign:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 3);
            break;
          case DesignProcedure.CompositeColumnDesign:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 13);
            break;
          case DesignProcedure.SteelFrameDesign:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 1);
            break;
          case DesignProcedure.ConcreteFrameDesign:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 2);
            break;
          case DesignProcedure.SteelJoistDesign:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 4);
            break;
          case DesignProcedure.NoDesign:
            Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 7);
            break;
        }
      }
      else
      {
        Model.FrameObj.SetDesignProcedure(CSIelement1D.name, 0);
      }
    }
  }
}
