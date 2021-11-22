using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Objects.Structural.ETABS.Properties;
using Speckle.Core.Models;
using StructuralUtilities.PolygonMesher;
using System.Linq;
using Objects.Structural.ETABS.Geometry;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
    public object AreaToNative(ETABSElement2D area)
    {
      if (GetAllAreaNames(Model).Contains(area.name))
      {
        return null;
      }
      string name = "";
      int numPoints = area.topology.Count();
      List<double> X = new List<double> { };
      List<double> Y = new List<double> { };
      List<double> Z = new List<double> { };


      foreach (Node point in area.topology)
      {
        X.Add(point.basePoint.x);
        Y.Add(point.basePoint.y);
        Z.Add(point.basePoint.z);
      }
      double[] x = X.ToArray();
      double[] y = Y.ToArray();
      double[] z = Z.ToArray();

 
      if (area.property != null)
      {
        Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name, area.property.name);

      }
      else
      {
        Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name);

      }
      if (area.name != null)
      {
        Model.AreaObj.ChangeName(name, area.name);
      }
      double[] values = null;
      if(area.modifiers != null){
        values = area.modifiers;
      }

      Model.AreaObj.SetModifiers(area.name, ref values);
      Model.AreaObj.SetLocalAxes(area.name, area.orientationAngle);

      return name;

    }
    public Element2D AreaToSpeckle(string name)
    {
      string units = ModelUnits();
      var speckleStructArea = new ETABSElement2D();

      speckleStructArea.name = name;
      int numPoints = 0;
      string[] points = null;
      Model.AreaObj.GetPoints(name, ref numPoints, ref points);
      List<Node> nodes = new List<Node>();
      foreach (string point in points)
      {
        Node node = PointToSpeckle(point);
        nodes.Add(node);
      }
      speckleStructArea.topology = nodes;
      string propName = "";
      Model.AreaObj.GetProperty(name, ref propName);
      speckleStructArea.property = Property2DToSpeckle(name, propName);

      List<double> coordinates = new List<double> { };
      foreach (Node node in nodes)
      {
        coordinates.Add(node.basePoint.x);
        coordinates.Add(node.basePoint.y);
        coordinates.Add(node.basePoint.z);
      }

      //Get orientation angle
      double angle = 0;
      bool advanced = true;
      Model.AreaObj.GetLocalAxes(name, ref angle, ref advanced);
      speckleStructArea.orientationAngle = angle;

      PolygonMesher polygonMesher = new PolygonMesher();
      polygonMesher.Init(coordinates);
      var faces = polygonMesher.Faces();
      var vertices = polygonMesher.Coordinates;
      speckleStructArea.displayMesh = new Geometry.Mesh(vertices, faces);

      //Model.AreaObj.GetModifiers(area, ref value);
      //speckleProperty2D.modifierInPlane = value[2];
      //speckleProperty2D.modifierBending = value[5];
      //speckleProperty2D.modifierShear = value[6];

      double[] values = null;
      Model.AreaObj.GetModifiers(name, ref values);
      speckleStructArea.modifiers = values;

      var GUID = "";
      Model.AreaObj.GetGUID(name, ref GUID);
      speckleStructArea.applicationId = GUID;
      List<Base> elements = SpeckleModel.elements;
      List<string> application_Id = elements.Select(o => o.applicationId).ToList();
      if (!application_Id.Contains(speckleStructArea.applicationId))
      {
        SpeckleModel.elements.Add(speckleStructArea);
      }


      return speckleStructArea;
    }

  }
}
