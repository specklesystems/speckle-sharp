using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    //NOTE: FaceWalls cannot be updated, as well we can't seem to get their base face easily so they are ToNatvie only
    public List<ApplicationPlaceholderObject> FaceWallToNative(RevitFaceWall speckleWall)
    {
      if (speckleWall.face == null)
      {
        throw new Exception("Only surface based FaceWalls are currently supported.");
      }


      var wallType = GetElementType<WallType>(speckleWall);
      Face f = BrepFaceToNative(speckleWall.face);

      var revitWall = DB.FaceWall.Create(Doc, wallType.Id, GetWallLocationLine(speckleWall.locationLine), f.Reference);

      if (revitWall == null)
      {
        ConversionErrors.Add(new Error { message = $"Failed to create wall ${speckleWall.applicationId}." });
        return null;
      }


      SetInstanceParameters(revitWall, speckleWall);

      var placeholders = new List<ApplicationPlaceholderObject>() {new ApplicationPlaceholderObject
      {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
      } };

      var hostedElements = SetHostedElements(speckleWall, revitWall);
      placeholders.AddRange(hostedElements);

      return placeholders;
    }
  }
}