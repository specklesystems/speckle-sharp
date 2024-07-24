using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Speckle.Core.Models;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using System.Linq;
using Speckle.Core.Kits;
using Objects.Structural.Properties;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public string LinkToNative(CSIElement1D link, IList<string>? notes)
  {
    PointToNative((CSINode)link.end1Node, notes);
    PointToNative((CSINode)link.end2Node, notes);
    string linkName = null;
    int numberProps = 0;
    string[] listProp = null;
    Model.PropLink.GetNameList(ref numberProps, ref listProp);
    if (listProp.Contains(link.property.name))
    {
      var success = Model.LinkObj.AddByPoint(
        link.end1Node.name,
        link.end2Node.name,
        ref linkName,
        PropName: link.property.name
      );
      if (success != 0)
      {
        throw new ConversionException("Failed to add new link by point");
      }

      return linkName;
    }
    else
    {
      string propertyName = LinkPropertyToNative((CSILinkProperty)link.property);
      Model.LinkObj.AddByPoint(link.end1Node.name, link.end2Node.name, ref linkName, PropName: propertyName);
      return propertyName;
    }
  }

  public CSIElement1D LinkToSpeckle(string name)
  {
    string units = ModelUnits();

    string pointI,
      pointJ;
    pointI = pointJ = null;
    _ = Model.LinkObj.GetPoints(name, ref pointI, ref pointJ);
    var pointINode = PointToSpeckle(pointI);
    var pointJNode = PointToSpeckle(pointJ);
    var speckleLine = new Line();
    if (units != null)
    {
      speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint, units);
    }
    else
    {
      speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint);
    }

    string linkProp = null;
    Model.LinkObj.GetProperty(name, ref linkProp);
    Property1D property = LinkPropertyToSpeckle(linkProp);

    var speckleStructLink = new CSIElement1D(speckleLine, property, ElementType1D.Link);

    double localAxis = 0;
    bool advanced = false;
    Model.LinkObj.GetLocalAxes(name, ref localAxis, ref advanced);
    speckleStructLink.orientationAngle = localAxis;

    speckleStructLink.name = name;
    speckleStructLink.end1Node = pointINode;
    speckleStructLink.end2Node = pointJNode;

    var GUID = "";
    Model.LinkObj.GetGUID(name, ref GUID);
    speckleStructLink.applicationId = GUID;
    List<Base> elements = SpeckleModel.elements;
    List<string> application_Id = elements.Select(o => o.applicationId).ToList();
    if (!application_Id.Contains(speckleStructLink.applicationId))
    {
      SpeckleModel.elements.Add(speckleStructLink);
    }

    return speckleStructLink;
  }
}
