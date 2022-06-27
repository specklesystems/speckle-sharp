using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Objects.Organization;
using Objects.Other;
using Speckle.Core.Models;

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
      if ( sendProjectInfo )
      {
        var info = ProjectInfoToSpeckle(doc.ProjectInformation);
        info.latitude = doc.SiteLocation.Latitude;
        info.longitude = doc.SiteLocation.Longitude;
        info.siteName = doc.SiteLocation.PlaceName;
        info.locations = ProjectLocationsToSpeckle(doc.ProjectLocations);
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
    public List<Base> ProjectLocationsToSpeckle(DB.ProjectLocationSet locations)
    {
      // TODO: do we need a location obj?
      var spcklLocations = new List<Base>();
      foreach ( DB.ProjectLocation location in locations )
      {
        var position = location.GetProjectPosition(DB.XYZ.Zero);
        var revitTransform = DB.Transform.CreateRotation(DB.XYZ.BasisZ, position.Angle);

        var spcklLoc = new Base() { applicationId = location.UniqueId };
        spcklLoc[ "transform" ] = new Transform(
          new[ ] { revitTransform.BasisX[ 0 ], revitTransform.BasisX[ 1 ], revitTransform.BasisX[ 2 ] },
          new[ ] { revitTransform.BasisY[ 0 ], revitTransform.BasisY[ 1 ], revitTransform.BasisY[ 2 ] },
          new[ ] { revitTransform.BasisZ[ 0 ], revitTransform.BasisZ[ 1 ], revitTransform.BasisZ[ 2 ] },
          new[ ] { ScaleToSpeckle(position.EastWest), ScaleToSpeckle(position.NorthSouth), ScaleToSpeckle(position.Elevation) },
          ModelUnits);
        spcklLoc[ "name" ] = location.Name;
        spcklLoc[ "trueNorth" ] = position.Angle;
        spcklLocations.Add(spcklLoc);
      }

      return spcklLocations;
    }
  }
}