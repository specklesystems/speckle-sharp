using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Speckle.Core.Models;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using System.Linq;
using CSiAPIv1;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void UpdateFrame(Element1D element1D, string name, ref ApplicationObject appObj)
    {
      var end1node = element1D.end1Node?.basePoint ?? element1D.baseLine?.start;
      var end2node = element1D.end2Node?.basePoint ?? element1D.baseLine?.end;

      if (end1node == null || end2node == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Frame {element1D.name} does not have valid endpoints");
        return;
      }

      UpdateFrameLocation(name, end1node, end2node, appObj);
      SetFrameElementProperties(element1D, name);
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
          Model.PointObj.DeleteSpecialPoint(pt1);
        Model.PointObj.GetConnectivity(pt2, ref numItems, ref objTypes, ref objNames, ref pointNums);
        if (numItems == 0)
          Model.PointObj.DeleteSpecialPoint(pt2);
      }

      if (success == 0)
      {
        string guid = null;
        Model.FrameObj.GetGUID(name, ref guid);
        appObj.Update(status: ApplicationObject.State.Updated, createdId: guid, convertedItem: $"Frame{delimiter}{name}");
      }
      else
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Failed to change frame connectivity");
    }

    public void FrameToNative(Element1D element1D, ref ApplicationObject appObj)
    {
      if (element1D.type == ElementType1D.Link)
      {
        LinkToNative((CSIElement1D)element1D, ref appObj);
        return;
      }

      if (ElementExistsWithApplicationId(element1D.applicationId, out string name))
      {
        UpdateFrame(element1D, name, ref appObj);
        return;
      }

      Line baseline = element1D.baseLine;
      string[] properties = new string[] { };
      int number = 0;
      Model.PropFrame.GetNameList(ref number, ref properties);
      if (!properties.Contains(element1D.property.name))
      {
        Property1DToNative(element1D.property, ref appObj);
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

      CreateFrame(end1node, end2node, out var newFrame, out var _, ref appObj);
      SetFrameElementProperties(element1D, newFrame);
    }

    public int CreateFrame(Point p0, Point p1, out string newFrame, out string guid, ref ApplicationObject appObj, string type = "Default", string nameOverride = null)
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

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: guid, convertedItem: $"Frame{delimiter}{newFrame}");
      else
        appObj.Update(status: ApplicationObject.State.Failed);

      return success;
    }

    public CSIElement1D FrameToSpeckle(string name)
    {
      string units = ModelUnits();

      var speckleStructFrame = new CSIElement1D();

      speckleStructFrame.name = name;
      string pointI, pointJ;
      pointI = pointJ = null;
      _ = Model.FrameObj.GetPoints(name, ref pointI, ref pointJ);
      var pointINode = PointToSpeckle(pointI);
      var pointJNode = PointToSpeckle(pointJ);
      speckleStructFrame.end1Node = pointINode;
      speckleStructFrame.end2Node = pointJNode;
      var speckleLine = new Line();
      if (units != null)
      {
        speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint, units);
      }
      else
      {
        speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint);
      }
      speckleStructFrame.baseLine = speckleLine;
      eFrameDesignOrientation frameDesignOrientation = eFrameDesignOrientation.Null;
      Model.FrameObj.GetDesignOrientation(name, ref frameDesignOrientation);
      switch (frameDesignOrientation)
      {
        case eFrameDesignOrientation.Column:
          {
            speckleStructFrame.type = ElementType1D.Column;
            break;
          }
        case eFrameDesignOrientation.Beam:
          {
            speckleStructFrame.type = ElementType1D.Beam;
            break;
          }
        case eFrameDesignOrientation.Brace:
          {
            speckleStructFrame.type = ElementType1D.Brace;
            break;
          }
        case eFrameDesignOrientation.Null:
          {
            //speckleStructFrame.memberType = MemberType.Generic1D;
            speckleStructFrame.type = ElementType1D.Null;
            break;
          }
        case eFrameDesignOrientation.Other:
          {
            speckleStructFrame.type = ElementType1D.Other;
            break;
          }
      }

      bool[] iRelease, jRelease;
      iRelease = jRelease = null;
      double[] startV, endV;
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


      string property, SAuto;
      property = SAuto = null;
      Model.FrameObj.GetSection(name, ref property, ref SAuto);
      speckleStructFrame.property = Property1DToSpeckle(property);

      double offSetEnd1 = 0;
      double offSetEnd2 = 0;
      double RZ = 0;
      bool autoOffSet = true;
      Model.FrameObj.GetEndLengthOffset(name, ref autoOffSet, ref offSetEnd1, ref offSetEnd2, ref RZ);
      //Offset needs to be oriented wrt to 1-axis
      Vector end1Offset = new Vector(0, 0, offSetEnd1, ModelUnits());
      Vector end2Offset = new Vector(0, 0, offSetEnd2, ModelUnits());
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
      double[] modifiers = new double[] { };
      int s = Model.FrameObj.GetModifiers(name, ref modifiers);
      if (s == 0) { speckleStructFrame.Modifiers = modifiers; }

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

    public void SetFrameElementProperties(Element1D element1D, string newFrame)
    {
      bool[] end1Release = null;
      bool[] end2Release = null;
      double[] startV, endV;
      startV = null;
      endV = null;
      if (element1D.end1Releases != null && element1D.end2Releases != null)
      {
        end1Release = RestraintToNative(element1D.end1Releases);
        end2Release = RestraintToNative(element1D.end2Releases);
        startV = PartialRestraintToNative(element1D.end1Releases);
        endV = PartialRestraintToNative(element1D.end2Releases);
      }

      var propAppObj = new ApplicationObject(element1D.applicationId, element1D.speckle_type);
      var propertyName = Property1DToNative(element1D.property, ref propAppObj);
      if (propertyName != null)
        Model.FrameObj.SetSection(newFrame, propertyName);

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
        if (CSIelement1D.SpandrelAssignment != null) { Model.FrameObj.SetSpandrel(CSIelement1D.name, CSIelement1D.SpandrelAssignment); }
        if (CSIelement1D.PierAssignment != null) { Model.FrameObj.SetPier(CSIelement1D.name, CSIelement1D.PierAssignment); }
        if (CSIelement1D.CSILinearSpring != null) { Model.FrameObj.SetSpringAssignment(CSIelement1D.name, CSIelement1D.CSILinearSpring.name); }
        if (CSIelement1D.Modifiers != null)
        {
          var modifiers = CSIelement1D.Modifiers;
          Model.FrameObj.SetModifiers(CSIelement1D.name, ref modifiers);
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
}
