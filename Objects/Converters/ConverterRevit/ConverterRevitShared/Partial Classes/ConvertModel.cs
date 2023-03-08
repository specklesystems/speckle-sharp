using System.Collections.Generic;
using Autodesk.Revit.Creation;
using Objects.Geometry;
using Objects.Organization;
using Objects.Other;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Transform = Objects.Other.Transform;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Returns a <see cref="Model"/> object containing <see cref="Objects.BuiltElements.Revit.ProjectInfo"/> and location
    /// information. This is intended to be used as the root commit object when sending to Speckle.
    /// </summary>
    /// <param name="doc">the currently active document</param>
    /// <param name="sendProjectInfo">true if project info should be added</param>
    /// <returns></returns>
    public Base ModelToSpeckle(DB.Document doc, bool sendProjectInfo = true)
    {
      var model = new Model();
      // TODO: setting for whether or not to include project info
      if (sendProjectInfo)
      {
        var info = ProjectInfoToSpeckle(doc.ProjectInformation);
        info.latitude = doc.SiteLocation.Latitude;
        info.longitude = doc.SiteLocation.Longitude;
        info.siteName = doc.SiteLocation.PlaceName;
        info.locations = ProjectLocationsToSpeckle(doc);
        model.info = info;
      }

      Report.Log($"Created Model Object");

      return model;
    }

    /// <summary>
    /// Converts the Revit document's <see cref="DB.ProjectLocationSet"/> into a list of bases including the <see cref="Objects.Other.Transform"/>,
    /// name, and true north angle (in radians)
    /// </summary>
    /// <param name="locations"></param>
    /// <returns></returns>
    public List<Base> ProjectLocationsToSpeckle(DB.Document doc)
    {
      var locations = doc.ProjectLocations;
      // TODO: do we need a location obj?
      var spcklLocations = new List<Base>();
      foreach (DB.ProjectLocation location in locations)
      {
        var position = location.GetProjectPosition(DB.XYZ.Zero);
        var revitTransform = DB.Transform.CreateRotation(DB.XYZ.BasisZ, position.Angle);

        var spcklLoc = new Base() { applicationId = location.UniqueId };
        var basisX = VectorToSpeckle(revitTransform.BasisX, doc);
        var basisY = VectorToSpeckle(revitTransform.BasisY, doc);
        var basisZ = VectorToSpeckle(revitTransform.BasisZ, doc);
        var translation = new Vector(ScaleToSpeckle(position.EastWest), ScaleToSpeckle(position.NorthSouth), ScaleToSpeckle(position.Elevation), ModelUnits);
        spcklLoc["transform"] = new Transform(basisX, basisY, basisZ, translation);
        spcklLoc["name"] = location.Name;
        spcklLoc["trueNorth"] = position.Angle;
        spcklLocations.Add(spcklLoc);
      }

      return spcklLocations;
    }
  }
}
