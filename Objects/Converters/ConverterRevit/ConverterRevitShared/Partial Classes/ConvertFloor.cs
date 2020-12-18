
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Opening = Objects.BuiltElements.Opening;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> FloorToNative(BuiltElements.Floor speckleFloor)
    {
      if (speckleFloor.outline == null)
      {
        throw new Exception("Only outline based Floor are currently supported.");
      }

      bool structural = false;
      var outline = CurveToNative(speckleFloor.outline);

      DB.Level level;

      if (speckleFloor is RevitFloor speckleRevitFloor)
      {
        level = LevelToNative(speckleRevitFloor.level);
        structural = speckleRevitFloor.structural;
      }
      else
      {
        level = LevelToNative(LevelFromCurve(outline.get_Item(0)));
      }

      var floorType = GetElementType<FloorType>(speckleFloor);

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element. The closest thing would be:
      // https://adndevblog.typepad.com/aec/2013/10/change-the-boundary-of-floorsslabs.html
      // This would only work if the floors have the same number (and type!!!) of outline curves. 
      var docObj = GetExistingElementByApplicationId(speckleFloor.applicationId);
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.Floor revitFloor;
      if (floorType == null)
      {
        revitFloor = Doc.Create.NewFloor(outline, structural);
      }
      else
      {
        revitFloor = Doc.Create.NewFloor(outline, floorType, level, structural);
      }

      Doc.Regenerate();

      try
      {
        CreateVoids(revitFloor, speckleFloor);
      }
      catch (Exception ex)
      {
        ConversionErrors.Add(new Error($"Could not create openings in floor {speckleFloor.applicationId}", ex.Message));
      }

      SetInstanceParameters(revitFloor, speckleFloor);

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleFloor.applicationId, ApplicationGeneratedId = revitFloor.UniqueId, NativeObject = revitFloor } };

      var hostedElements = SetHostedElements(speckleFloor, revitFloor);
      placeholders.AddRange(hostedElements);

      return placeholders;
    }

    //a floor outline can have "voids/holes" for 3 reasons:
    // - there is a shaft cutting through it > we don't need to create an opening (the shaft will be created on its own)
    // - there is a vertical opening cutting through it > we don't need to create an opening (the opening will be created on its own)
    // - the floor profile was modeled with holes > we need to create an openeing as the Revit API doesn't let us generate it with holes!
    private void CreateVoids(DB.Floor floor, Base speckleElement)
    {
      if (speckleElement["voids"] == null || !(speckleElement["voids"] is List<ICurve>))
        return;

      //list of openings hosted in this speckle element
      var openings = new List<RevitOpening>();
      if (speckleElement["elements"] != null && (speckleElement["elements"] is List<Base> elements))
        openings.AddRange(elements.Where(x => x is RevitVerticalOpening).Cast<RevitVerticalOpening>());

      //list of shafts part of this conversion set
      openings.AddRange(ContextObjects.Where(x => x.NativeObject is RevitShaft).Select(x => x.NativeObject).Cast<RevitShaft>());

      foreach (var @void in speckleElement["voids"] as List<ICurve>)
      {
        if (HasOverlappingOpening(@void, openings))
          continue;

        var curveArray = CurveToNative(@void);
        Doc.Create.NewOpening(floor, curveArray, true);
      }
    }

    private bool HasOverlappingOpening(ICurve @void, List<RevitOpening> openings)
    {
      foreach (RevitOpening opening in openings)
      {
        if (CurvesOverlap(@void, opening.outline))
          return true;
      }
      return false;

    }

    private bool CurvesOverlap(ICurve icurveA, ICurve icurveB)
    {
      var curveArrayA = CurveToNative(icurveA).Cast<DB.Curve>().ToList();
      var curveArrayB = CurveToNative(icurveB).Cast<DB.Curve>().ToList();

      //we need to account for various scenarios, eg a shaft might be made of multiple shapes
      //while the resulting cut in the floor will only be made on a single shape, so we need to cross check them all
      foreach (var curveA in curveArrayA)
      {
        //move curves to Z = 0, needed for shafts!
        curveA.MakeBound(0, 1);
        var z = curveA.GetEndPoint(0).Z;
        var cA = curveA.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, -z)));

        foreach (var curveB in curveArrayB)
        {
          //move curves to Z = 0, needed for shafts!
          curveB.MakeBound(0, 1);
          z = curveB.GetEndPoint(0).Z;
          var cB = curveB.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, -z)));

          var result = cA.Intersect(cB);
          if (result != SetComparisonResult.BothEmpty && result != SetComparisonResult.Disjoint)
            return true;

        }
      }

      return false;
    }





    private RevitFloor FloorToSpeckle(DB.Floor revitFloor)
    {
      var profiles = GetProfiles(revitFloor);

      var speckleFloor = new RevitFloor();
      speckleFloor.type = Doc.GetElement(revitFloor.GetTypeId()).Name;
      speckleFloor.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleFloor.voids = profiles.Skip(1).ToList();
      }

      speckleFloor.level = ConvertAndCacheLevel(revitFloor, BuiltInParameter.LEVEL_PARAM);
      speckleFloor.structural = GetParamValue<bool>(revitFloor, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);

      GetRevitParameters(speckleFloor, revitFloor, new List<string> { "LEVEL_PARAM", "FLOOR_PARAM_IS_STRUCTURAL" });

      var mesh = new Geometry.Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrayFromElement(revitFloor, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      speckleFloor["@displayMesh"] = mesh;

      GetHostedElements(speckleFloor, revitFloor);

      return speckleFloor;
    }

    //Nesting the various profiles into a polycurve segments
    private List<ICurve> GetProfiles(DB.CeilingAndFloor floor)
    {
      var profiles = new List<ICurve>();
      var faces = HostObjectUtils.GetTopFaces(floor);
      Face face = floor.GetGeometryObjectFromReference(faces[0]) as Face;
      var crvLoops = face.GetEdgesAsCurveLoops();
      foreach (var crvloop in crvLoops)
      {
        var poly = new Polycurve(ModelUnits);
        foreach (var curve in crvloop)
        {
          var c = curve;

          if (c == null)
          {
            continue;
          }

          poly.segments.Add(CurveToSpeckle(c));
        }
        profiles.Add(poly);
      }
      return profiles;
    }
  }
}
