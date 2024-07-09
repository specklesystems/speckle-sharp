using System;
using System.Collections.Generic;
using Objects.GIS;
using Speckle.Core.Models;
using RH = Rhino.DocObjects;
using System.Linq;
using Rhino.Geometry;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // polygon element
  // NOTE: class no longer in use? from 2.19
  public ApplicationObject PolygonElementToNative(PolygonElement poly)
  {
    var appObj = new ApplicationObject(poly.id, poly.speckle_type) { applicationId = poly.applicationId };

    // get the group name
    var commitInfo = GetCommitInfo();
    string groupName = $"{commitInfo} - " + poly.id;
    if (Doc.Groups.FindName(groupName) is RH.Group existingGroup)
    {
      Doc.Groups.Delete(existingGroup);
    }

    List<Guid> addedGeometry = new();
    foreach (object geo in poly.geometry)
    {
      if (geo is Base geoBase)
      {
        var display = geoBase["displayValue"] as List<object> ?? geoBase["@displayValue"] as List<object>;
        if (display is null)
        {
          continue;
        }

        foreach (object displayObject in display)
        {
          if (displayObject is Base baseObj)
          {
            if (ConvertToNative(baseObj) is GeometryBase convertedObject)
            {
              Guid id = Doc.Objects.Add(convertedObject);
              if (id != Guid.Empty)
              {
                addedGeometry.Add(id);
              }
            }
          }
        }
      }
    }

    if (addedGeometry.Count == 0)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "No objects were created for group");
      return appObj;
    }

    int groupIndex = Doc.Groups.Add(groupName, addedGeometry);
    if (groupIndex == -1)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add group to doc");
      return appObj;
    }

    RH.Group convertedGroup = Doc.Groups.FindIndex(groupIndex);

    // update appobj
    appObj.Update(convertedItem: convertedGroup, createdIds: addedGeometry.Select(o => o.ToString()).ToList());

    return appObj;
  }

  // gis feature
  public ApplicationObject GisFeatureToNative(GisFeature feature)
  {
    var appObj = new ApplicationObject(feature.id, feature.speckle_type) { applicationId = feature.applicationId };

    // get the group name
    var commitInfo = GetCommitInfo();
    string groupName = $"{commitInfo} - " + feature.id;
    if (Doc.Groups.FindName(groupName) is RH.Group existingGroup)
    {
      Doc.Groups.Delete(existingGroup);
    }

    // for gis features, we are assuming that the `displayValue prop` should be converted first
    // if there are no objects in `displayValue`, then we will fall back to check for convertible objects in `geometries`
    List<GeometryBase> convertedObjects = new();
    if (feature.displayValue is List<Base> displayValue && displayValue.Count > 0)
    {
      foreach (Base displayObj in displayValue)
      {
        if (ConvertToNative(displayObj) is GeometryBase convertedObject)
        {
          convertedObjects.Add(convertedObject);
        }
      }
    }
    else if (feature.geometry is List<Base> geometries && geometries.Count > 0)
    {
      foreach (Base displayObj in geometries)
      {
        if (ConvertToNative(displayObj) is GeometryBase convertedObject)
        {
          convertedObjects.Add(convertedObject);
        }
      }
    }
    else
    {
      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: "No objects in displayValue or geometries was found"
      );
      return appObj;
    }

    List<Guid> addedGeometry = new();
    foreach (GeometryBase convertedObject in convertedObjects)
    {
      Guid id = Doc.Objects.Add(convertedObject);
      if (id != Guid.Empty)
      {
        addedGeometry.Add(id);
      }
    }

    if (addedGeometry.Count == 0)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "No objects were created for group");
      return appObj;
    }

    int groupIndex = Doc.Groups.Add(groupName, addedGeometry);
    if (groupIndex == -1)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add group to doc");
      return appObj;
    }

    RH.Group convertedGroup = Doc.Groups.FindIndex(groupIndex);

    // update appobj
    appObj.Update(convertedItem: convertedGroup, createdIds: addedGeometry.Select(o => o.ToString()).ToList());

    return appObj;
  }
}
