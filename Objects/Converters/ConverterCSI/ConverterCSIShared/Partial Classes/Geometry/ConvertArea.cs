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
using Objects.Structural.Properties;
using Objects.Geometry;
using ConverterCSIShared.Extensions;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public object updateExistingArea(Element2D area)
    {
      string GUID = "";
      Model.AreaObj.GetGUID(area.name, ref GUID);
      if (area.applicationId == GUID)
      {
        SetAreaProperties(area.name, area);
      }
      return area.name;
    }
    public void UpdateArea(Element2D area, string name, ref ApplicationObject appObj)
    {
      var points = new string[0];
      var numPoints = 0;
      Model.AreaObj.GetPoints(name, ref numPoints, ref points);

      var pointsUpdated = new List<string>();
      bool connectivityChanged = false;
      if (points.Length != area.topology.Count)
        connectivityChanged = true;

      for (int i = 0; i < area.topology.Count; i++)
      {
        if (i >= points.Length)
        {
          CreatePoint(area.topology[i].basePoint, out string pointName);
          pointsUpdated.Add(pointName);
          continue;
        }
        
        pointsUpdated.Add(UpdatePoint(points[i], area.topology[i]));
        if (!connectivityChanged && pointsUpdated[i] != points[i])
          connectivityChanged = true;
      }

      int success = 0;
      int numErrorMsgs = 0;
      string importLog = "";
      if (connectivityChanged)
      {
#if SAP2000
        var refArray = pointsUpdated.ToArray();
        success = Model.EditArea.ChangeConnectivity(name, pointsUpdated.Count, ref refArray);
#else
        int tableVersion = 0;
        int numberRecords = 0;
        string[] fieldsKeysIncluded = null;
        string[] tableData = null;
        string floorTableKey = "Floor Object Connectivity";
        Model.DatabaseTables.GetTableForEditingArray(floorTableKey, "ThisParamIsNotActiveYet", ref tableVersion, ref fieldsKeysIncluded, ref numberRecords, ref tableData);

        // if the floor object now has more points than it previously had
        // and it has more points that any other floor object, then updating would involve adding a new column to this array
        // for the moment, forget that, it feels a bit fragile. Just delete the current object and remake it with the same GUID
        if (pointsUpdated.Count > points.Length && pointsUpdated.Count > fieldsKeysIncluded.Length - 2)
        {
          string GUID = "";
          Model.AreaObj.GetGUID(name, ref GUID);
          var updatedArea = AreaToSpeckle(name);

          updatedArea.applicationId = GUID;
          updatedArea.topology = area.topology;

          Model.AreaObj.Delete(name);
          ExistingObjectGuids.Remove(GUID);
          var dummyAppObj = new ApplicationObject(null, null);
          AreaToNative(updatedArea, ref dummyAppObj);
          if (dummyAppObj.Status != ApplicationObject.State.Created)
            success = 1;
        }
        else
        {
          for (int record = 0; record < numberRecords; record++)
          {
            if (tableData[record * fieldsKeysIncluded.Length] != name)
              continue;

            for (int i = 0; i < pointsUpdated.Count; i++)
              tableData[record * fieldsKeysIncluded.Length + (i + 1)] = pointsUpdated[i];
            break;
          }

          // this is a workaround for a CSI bug. The applyEditedTables is looking for "Unique Name", not "UniqueName"
          // this bug is patched in version 20.0.0
          if (ProgramVersion.CompareTo("20.0.0") < 0 && fieldsKeysIncluded[0] == "UniqueName")
            fieldsKeysIncluded[0] = "Unique Name";

          Model.DatabaseTables.SetTableForEditingArray(floorTableKey, ref tableVersion, ref fieldsKeysIncluded, numberRecords, ref tableData);

          int numFatalErrors = 0;
          int numWarnMsgs = 0;
          int numInfoMsgs = 0;
          success = Model.DatabaseTables.ApplyEditedTables(true, ref numFatalErrors, ref numErrorMsgs, ref numWarnMsgs, ref numInfoMsgs, ref importLog);

          if (success == 0)
          {
            int numItems = 0;
            int[] objTypes = null;
            string[] objNames = null;
            int[] pointNums = null;
            foreach (var node in points)
            {
              Model.PointObj.GetConnectivity(node, ref numItems, ref objTypes, ref objNames, ref pointNums);
              if (numItems == 0)
                Model.PointObj.DeleteSpecialPoint(node);
            }
          }
        }
#endif
      }

      SetAreaProperties(name, area);
      if (success == 0)
      {
        string guid = null;
        Model.AreaObj.GetGUID(name, ref guid);
        appObj.Update(status: ApplicationObject.State.Updated, createdId: guid, convertedItem: $"Area{delimiter}{name}");
        if (numErrorMsgs != 0)
          appObj.Update(log: new List<string>() { $"Area may not have updated successfully. Number of error messages for operation is {numErrorMsgs}", importLog });
      }
      else
        appObj.Update(status: ApplicationObject.State.Failed, log: new List<string>() { "Failed to apply edited database tables", importLog });
    }
    public void AreaToNative(Element2D area, ref ApplicationObject appObj)
    {
      if (ElementExistsWithApplicationId(area.applicationId, out string areaName))
      {
        UpdateArea(area, areaName, ref appObj);
        return;
      }

      if (GetAllAreaNames(Model).Contains(area.name))
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"There is already a frame object named {area.name} in the model");
        return;
      }

      var propName = CreateOrGetProp(area.property, out bool isExactMatch);
      if (!isExactMatch)
      {
        appObj.Update(logItem: $"Area section for object could not be created and was replaced with section named \"{propName}\"");
      }

      var name = CreateAreaFromPoints(area.topology.Select(t => t.basePoint), propName, out var success);
      SetAreaProperties(name, area);

      if (area.openings?.Count > 0)
      {
        foreach (var opening in area.openings)
        {
          string openingName;
          try
          {
            openingName = CreateAreaFromPoints(opening.ToPoints(), propName, out _);
          }
          catch
          {
            openingName = string.Empty;
          }
          var openingSuccess = Model.AreaObj.SetOpening(openingName, true);
          if (openingSuccess != 0)
          {
            appObj.Update(logItem: $"unable to create opening with id {opening.id}");
          }
        }
      }

      if (!string.IsNullOrEmpty(area.applicationId))
        Model.AreaObj.SetGUID(name, area.applicationId);

      var guid = "";
      Model.AreaObj.GetGUID(name, ref guid);

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: guid, convertedItem: $"Area{delimiter}{name}");
      else
        appObj.Update(status: ApplicationObject.State.Failed);
    }

    private string CreateAreaFromPoints(IEnumerable<Point> points, string propName, out int success)
    {
      var name = "";
      int numPoints = 0;
      var X = new List<double> { };
      var Y = new List<double> { };
      var Z = new List<double> { };

      foreach (var point in points)
      {
        X.Add(ScaleToNative(point.x, point.units));
        Y.Add(ScaleToNative(point.y, point.units));
        Z.Add(ScaleToNative(point.z, point.units));
        numPoints++;
      }

      if (Math.Abs(X.Last() - X.First()) < .01 
        && Math.Abs(Y.Last() - Y.First()) < .01
        && Math.Abs(Z.Last() - Z.First()) < .01
      )
      {
        X.RemoveAt(X.Count- 1);
        Y.RemoveAt(Y.Count- 1);
        Z.RemoveAt(Z.Count- 1);
        numPoints--;
      }
      var x = X.ToArray();
      var y = Y.ToArray();
      var z = Z.ToArray();

      success = Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name, propName);

      return name;
    }

    private string CreateOrGetProp(Property2D property, out bool isExactMatch)
    {
      int numberNames = 0;
      string[] propNames = null;
      Model.PropArea.GetNameList(ref numberNames, ref propNames);
      isExactMatch = true;

      if (propNames.Contains(property?.name))
      {
        return property.name;
      }
      else if (property is CSIProperty2D prop2D)
      {
        try
        {
          return Property2DToNative(prop2D);
        }
        catch (Exception)
        {
          // something failed... replace the type
        }
      }

      isExactMatch = false;
      if (propNames.Any())
      {
        // TODO: support creating of Property2D
        return propNames.First();
      }

      throw new Exception("Cannot create area because there aren't any area sections defined in the project file");
    }

    public void SetAreaProperties(string name, Element2D area)
    {
      if (!string.IsNullOrEmpty(area.name))
      {
        if (GetAllAreaNames(Model).Contains(area.name))
          area.name = area.id;
        Model.AreaObj.ChangeName(name, area.name);
        name = area.name;
      }

      Model.AreaObj.SetProperty(name, area.property?.name);
      if (area is CSIElement2D csiArea)
      {
        double[] values = null;
        if (csiArea.modifiers != null)
        {
          values = csiArea.modifiers;
        }

        Model.AreaObj.SetModifiers(csiArea.name, ref values);
        Model.AreaObj.SetLocalAxes(csiArea.name, csiArea.orientationAngle);
        Model.AreaObj.SetPier(csiArea.name, csiArea.PierAssignment);
        Model.AreaObj.SetSpandrel(csiArea.name, csiArea.SpandrelAssignment);
        if (csiArea.CSIAreaSpring != null) { Model.AreaObj.SetSpringAssignment(csiArea.name, csiArea.CSIAreaSpring.name); }

        if (csiArea.DiaphragmAssignment != null) 
        { 
          Model.AreaObj.SetDiaphragm(csiArea.name, csiArea.DiaphragmAssignment); 
        }
      }
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
      if (coordinates.Count != 0)
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
      List<Base> elements = SpeckleModel == null ? new List<Base>() : SpeckleModel.elements;
      List<string> application_Id = elements.Select(o => o.applicationId).ToList();
      if (!application_Id.Contains(speckleStructArea.applicationId))
      {
        SpeckleModel?.elements.Add(speckleStructArea);
      }

      // Should discretize between wall and slab types
      //speckleStructArea.memberType = MemberType.Generic2D;

      return speckleStructArea;
    }

  }
}
