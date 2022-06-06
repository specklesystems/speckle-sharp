using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;
using StructuralUtilities.PolygonMesher;
using System.Linq;
using Objects.Structural.CSI.Geometry;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public object AreaToNative(Element2D area)
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

      if (area.property is CSIOpening)
      {
        Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name);
        Model.AreaObj.SetOpening(name, true);
      }
      else if (area.property != null)
      {
        int numberNames = 0;
        string[] propNames = null;
        Model.PropArea.GetNameList(ref numberNames, ref propNames);
        if (propNames.Contains(area.property.name))
        {
          Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name, area.property.name);
        }
        else
        {
          Property2DToNative((CSIProperty2D)area.property);
          Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name, area.property.name);
        }
      }
      else
      {
        Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name);

      }
      if (area.name != null)
      {
        Model.AreaObj.ChangeName(name, area.name);
      }
      else
      {
        Model.AreaObj.ChangeName(name, area.id);
      }
      if (area is CSIElement2D)
      {
        var CSIarea = (CSIElement2D)area;
        double[] values = null;
        if (CSIarea.modifiers != null)
        {
          values = CSIarea.modifiers;
        }

        Model.AreaObj.SetModifiers(CSIarea.name, ref values);
        Model.AreaObj.SetLocalAxes(CSIarea.name, CSIarea.orientationAngle);
        Model.AreaObj.SetPier(CSIarea.name, CSIarea.PierAssignment);
        Model.AreaObj.SetSpandrel(CSIarea.name, CSIarea.SpandrelAssignment);
        if (CSIarea.CSIAreaSpring != null) { Model.AreaObj.SetSpringAssignment(CSIarea.name, CSIarea.CSIAreaSpring.name); }

        if (CSIarea.DiaphragmAssignment != null) { Model.AreaObj.SetDiaphragm(CSIarea.name, CSIarea.DiaphragmAssignment); }

      }


      return name;

    }
    public Element2D AreaToSpeckle(string name)
    {
      string units = ModelUnits();
      var speckleStructArea = new CSIElement2D();

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

      bool isOpening = false;
      Model.AreaObj.GetOpening(name, ref isOpening);
      if (isOpening == true)
      {
        speckleStructArea.property = new CSIOpening(true);
      }
      else
      {
        string propName = "";
        Model.AreaObj.GetProperty(name, ref propName);
        speckleStructArea.property = Property2DToSpeckle(name, propName);
      }


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
      if(coordinates.Count != 0)
      {
        PolygonMesher polygonMesher = new PolygonMesher();
        polygonMesher.Init(coordinates);
        var faces = polygonMesher.Faces();
        var vertices = polygonMesher.Coordinates;
        //speckleStructArea.displayMesh = new Geometry.Mesh(vertices, faces.ToArray(), null, null, ModelUnits(), null);
        speckleStructArea.displayValue = new List<Geometry.Mesh> { new Geometry.Mesh(vertices.ToList(), faces, units: ModelUnits()) };
      }

      //Model.AreaObj.GetModifiers(area, ref value);
      //speckleProperty2D.modifierInPlane = value[2];
      //speckleProperty2D.modifierBending = value[5];
      //speckleProperty2D.modifierShear = value[6];

      double[] values = null;
      Model.AreaObj.GetModifiers(name, ref values);
      speckleStructArea.modifiers = values;

      string springArea = null;
      Model.AreaObj.GetSpringAssignment(name, ref springArea);
      if (springArea != null) { speckleStructArea.CSIAreaSpring = AreaSpringToSpeckle(springArea); }

      string pierAssignment = null;
      Model.AreaObj.GetPier(name, ref pierAssignment);
      if (pierAssignment != null)
      {
        speckleStructArea.PierAssignment = pierAssignment;
      }

      string spandrelAssignment = null;
      Model.AreaObj.GetSpandrel(name, ref spandrelAssignment);
      if (spandrelAssignment != null)
      {
        speckleStructArea.SpandrelAssignment = spandrelAssignment;
      }

      string diaphragmAssignment = null;
      Model.AreaObj.GetDiaphragm(name, ref diaphragmAssignment);
      if (diaphragmAssignment != null)
      {
        speckleStructArea.DiaphragmAssignment = diaphragmAssignment;
      }

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