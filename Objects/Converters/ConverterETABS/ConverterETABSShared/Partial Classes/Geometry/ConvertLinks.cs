using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using Objects.Structural.ETABS.Geometry;
using Objects.Structural.ETABS.Properties;
using System.Linq;
using ETABSv1;

namespace Objects.Converter.ETABS
{
public partial class ConverterETABS{ 
    public ETABSElement1D LinkToSpeckle(string name)
    {
      string units = ModelUnits();

      var speckleStructFrame = new ETABSElement1D();

      speckleStructFrame.name = name;
      string pointI, pointJ;
      pointI = pointJ = null;
      _ = Model.LinkObj.GetPoints(name, ref pointI, ref pointJ);
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

      double localAxis = 0;
      bool advanced = false;
      Model.LinkObj.GetLocalAxes(name, ref localAxis, ref advanced);
      speckleStructFrame.orientationAngle = localAxis;

      speckleStructFrame.type = ElementType1D.Link;
      string linkProp = null;
      Model.LinkObj.GetProperty(name, ref linkProp);
      speckleStructFrame.property = LinkPropertyToSpeckle(linkProp);

      var GUID = "";
      Model.LinkObj.GetGUID(name, ref GUID);
      speckleStructFrame.applicationId = GUID;
      List<Base> elements = SpeckleModel.elements;
      List<string> application_Id = elements.Select(o => o.applicationId).ToList();
      if (!application_Id.Contains(speckleStructFrame.applicationId))
      {
        SpeckleModel.elements.Add(speckleStructFrame);
      }


      return speckleStructFrame;
    }
  }
}
