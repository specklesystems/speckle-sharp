using System;
using System.Collections.Generic;
using RG = Rhino.Geometry;
using Objects.GIS;
using Speckle.Core.Models;
using RH = Rhino.DocObjects;
using System.Drawing;
using System.Linq;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // polygon element
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

        foreach (var displayObject in display)
        {
          if (displayObject is Objects.Geometry.Mesh mesh)
          {
            RG.Mesh convertedMesh = MeshToNative(mesh);

            // get attributes
            var attribute = new RH.ObjectAttributes();

            // display
            var renderMaterial = mesh[@"renderMaterial"] as Other.RenderMaterial;
            if (renderMaterial != null)
            {
              attribute.ObjectColor = Color.FromArgb(renderMaterial.diffuse);
              attribute.ColorSource = RH.ObjectColorSource.ColorFromObject;
            }

            // render material
            if (renderMaterial != null)
            {
              var material = RenderMaterialToNative(renderMaterial);
              attribute.MaterialIndex = GetMaterialIndex(material?.Name);
              attribute.MaterialSource = RH.ObjectMaterialSource.MaterialFromObject;
            }

            // add mesh to doc
            Guid id = Doc.Objects.Add(convertedMesh, attribute);
            if (id != Guid.Empty)
            {
              addedGeometry.Add(id);
            }
          }
        }
      }
    }

    if (addedGeometry.Count == 0)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "No meshes were created for group");
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
