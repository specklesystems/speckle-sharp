using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    //TODO: might need to clean this up and split the ConversionLog.Addic by beam, FI, etc...
    public ApplicationObject FamilyInstanceToNative(BuiltElements.Revit.FamilyInstance speckleFi)
    {
      XYZ basePoint = PointToNative(speckleFi.basePoint);
      DB.Level level = ConvertLevelToRevit(speckleFi.level, out ApplicationObject.State levelState);
      DB.FamilyInstance familyInstance = null;
      var isUpdate = false;

      var docObj = GetExistingElementByApplicationId(speckleFi.applicationId);
      var appObj = new ApplicationObject(speckleFi.id, speckleFi.speckle_type) { applicationId = speckleFi.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      if (!GetElementType<FamilySymbol>(speckleFi, appObj, out DB.FamilySymbol familySymbol))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familySymbol.FamilyName != revitType.FamilyName)
            Doc.Delete(docObj.Id);
          else
          {
            familyInstance = (DB.FamilyInstance)docObj;

            //NOTE: updating an element location is quite buggy in Revit!
            //Let's say the first time an element is created its base point/curve is @ 10m and the Level is @ 0m
            //the element will be created @ 0m
            //but when this element is updated (let's say with no changes), it will jump @ 10m (unless there is a level change)!
            //to avoid this behavior we're always setting the previous location Z coordinate when updating an element
            //this means the Z coord of an element will only be set by its Level 
            //and by additional parameters as sill height, base offset etc
            var newLocationPoint = new XYZ(basePoint.X, basePoint.Y, (familyInstance.Location as LocationPoint).Point.Z);

            (familyInstance.Location as LocationPoint).Point = newLocationPoint;

            // BAND AID FIX ALERT
            // this is one of the stranger issues I've encountered. When I set the location of a family instance
            // it mostly works fine, but every so often it goes to a different location than the one I set
            // it seems like just reassigning the location to the same thing we just assigned it to works
            // I don't know why this is happening
            if ((familyInstance.Location as LocationPoint).Point != newLocationPoint)
              (familyInstance.Location as LocationPoint).Point = newLocationPoint;

            // check for a type change
            if (speckleFi.type != null && speckleFi.type != revitType.Name)
              familyInstance.ChangeTypeId(familySymbol.Id);

            TrySetParam(familyInstance, BuiltInParameter.FAMILY_LEVEL_PARAM, level);
            TrySetParam(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, level);
          }
          isUpdate = true;
        }
        catch
        {
          //something went wrong, re-create it
        }
      }

      //create family instance
      if (familyInstance == null)
      {
        //If the current host element is not null, it means we're coming from inside a nested conversion. 
        if (CurrentHostElement != null)
        {
          if (level == null)
            level = Doc.GetElement(CurrentHostElement.LevelId) as Level;

          // there are two (i think) main types of hosted elements which can be found with family.familyplacementtype
          // the two placement types for hosted elements are onelevelbasedhosted and workplanebased

          if (familySymbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBasedHosted)
          {
            familyInstance = Doc.Create.NewFamilyInstance(basePoint, familySymbol, CurrentHostElement, level, StructuralType.NonStructural);
          }
          else if (familySymbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased)
          {
            if (CurrentHostElement == null)
            {
              appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Object is work plane based but does not have a host element");
              return appObj;
            }
            if (CurrentHostElement is Wall wall)
            {
              Doc.Regenerate();

              Options op = new Options();
              op.ComputeReferences = true;
              GeometryElement wallGeom = wall.get_Geometry(op);
              Reference faceRef = null;

              foreach (var geom in wallGeom)
              {
                if (geom is Solid solid)
                {
                  FaceArray faceArray = solid.Faces;

                  double planeDist = double.PositiveInfinity;
                  foreach (Face face in faceArray)
                  {
                    if (face is PlanarFace planarFace)
                    {
                      // some family instance base points may lie on the intersection of faces
                      // this makes it so family instance families can only be placed on the
                      // faces of walls
                      if (NormalsAlign(planarFace.FaceNormal, wall.Orientation))
                      {
                        double newPlaneDist = ComputePlaneDistance(planarFace.Origin, planarFace.FaceNormal, basePoint);
                        if (newPlaneDist < planeDist)
                        {
                          planeDist = newPlaneDist;
                          faceRef = planarFace.Reference;
                        }
                      }
                    }
                  }
                }
              }

              // last resort, just guess a face
              if (faceRef == null)
                faceRef = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Interior)[0];

              XYZ norm = new XYZ(0, 0, 0);
              familyInstance = Doc.Create.NewFamilyInstance(faceRef, basePoint, norm, familySymbol);

              // parameters
              IList<Parameter> cutVoidsParams = familySymbol.Family.GetParameters("Cut with Voids When Loaded");
              IList<Parameter> lvlParams = familyInstance.GetParameters("Schedule Level");

              if (cutVoidsParams.ElementAtOrDefault(0) != null && cutVoidsParams[0].AsInteger() == 1)
                InstanceVoidCutUtils.AddInstanceVoidCut(Doc, wall, familyInstance);
              if (lvlParams.ElementAtOrDefault(0) != null)
                lvlParams[0].Set(level.Id);
            }
            else if (CurrentHostElement is Floor floor)
            {
              // TODO: support hosted elements on floors. Should be very similar to above implementation
              appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Work Plane based families on floors to be supported soon");
              return appObj;
            }
          }
          else
          {
            appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Unsupported FamilyPlacementType {familySymbol.Family.FamilyPlacementType}");
            return appObj;
          }
          // try a catch all solution as a last resort
          if (familyInstance == null)
          {
            try
            {
              familyInstance = Doc.Create.NewFamilyInstance(basePoint, familySymbol, CurrentHostElement, level, StructuralType.NonStructural);
            }
            catch { }
          }
        }
        //Otherwise, proceed as normal.
        else
          familyInstance = Doc.Create.NewFamilyInstance(basePoint, familySymbol, level, StructuralType.NonStructural);
      }

      //required for face flipping to work!
      Doc.Regenerate();

      if (familyInstance.CanFlipHand && speckleFi.handFlipped != familyInstance.HandFlipped)
        familyInstance.flipHand();

      if (familyInstance.CanFlipFacing && speckleFi.facingFlipped != familyInstance.FacingFlipped)
        familyInstance.flipFacing();

      // NOTE: do not check for the CanRotate prop as it doesn't work (at least on some families I tried)!
      // some point based families don't have a rotation, so keep this in a try catch
      try
      {
        if (speckleFi.rotation != (familyInstance.Location as LocationPoint).Rotation)
        {
          var axis = DB.Line.CreateBound(new XYZ(basePoint.X, basePoint.Y, 0), new XYZ(basePoint.X, basePoint.Y, 1000));
          (familyInstance.Location as LocationPoint).Rotate(axis, speckleFi.rotation - (familyInstance.Location as LocationPoint).Rotation);
        }
      }
      catch { }

      SetInstanceParameters(familyInstance, speckleFi);
      if (speckleFi.mirrored)
        appObj.Update(logItem: $"Element with id {familyInstance.Id} should be mirrored, but a Revit API limitation prevented us from doing so.");

      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: familyInstance.UniqueId, convertedItem: familyInstance);
      return appObj;
    }

    /// <summary>
    /// Entry point for all revit family conversions.
    /// </summary>
    /// <param name="revitFi"></param>
    /// <returns></returns>
    public Base FamilyInstanceToSpeckle(DB.FamilyInstance revitFi, out List<string> notes)
    {
      notes = new List<string>();
      if (!ShouldConvertHostedElement(revitFi, revitFi.Host))
        return null;

      //adaptive components
      if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(revitFi))
        return AdaptiveComponentToSpeckle(revitFi);

      //these elements come when the curtain wall is generated
      //let's not send them to speckle unless we realize they are needed!
      if (Categories.curtainWallSubElements.Contains(revitFi.Category))
      {
        if (SubelementIds.Contains(revitFi.Id))
          return null;
        else
          //TODO: sort these so we consistently get sub-elements from the wall element in case also sub-elements are sent
          SubelementIds.Add(revitFi.Id);
      }

      //beams & braces
      if (Categories.beamCategories.Contains(revitFi.Category))
      {
        if (revitFi.StructuralType == StructuralType.Beam)
          return BeamToSpeckle(revitFi, out notes);
        else if (revitFi.StructuralType == StructuralType.Brace)
          return BraceToSpeckle(revitFi, out notes);
      }

      //columns
      if (Categories.columnCategories.Contains(revitFi.Category) || revitFi.StructuralType == StructuralType.Column)
        return ColumnToSpeckle(revitFi, out notes);

      var baseGeometry = LocationToSpeckle(revitFi);
      var basePoint = baseGeometry as Point;
      if (basePoint == null)
        return RevitElementToSpeckle(revitFi, out notes);

      var lev1 = ConvertAndCacheLevel(revitFi, BuiltInParameter.FAMILY_LEVEL_PARAM);
      var lev2 = ConvertAndCacheLevel(revitFi, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);

      var symbol = revitFi.Document.GetElement(revitFi.GetTypeId()) as FamilySymbol;

      var speckleFi = new BuiltElements.Revit.FamilyInstance();
      speckleFi.basePoint = basePoint;
      speckleFi.family = symbol.FamilyName;
      speckleFi.type = symbol.Name;
      speckleFi.category = revitFi.Category.Name;
      speckleFi.facingFlipped = revitFi.FacingFlipped;
      speckleFi.handFlipped = revitFi.HandFlipped;
      speckleFi.level = lev1 != null ? lev1 : lev2;
      speckleFi.mirrored = revitFi.Mirrored;

      if (revitFi.Location is LocationPoint)
        speckleFi.rotation = ((LocationPoint)revitFi.Location).Rotation;

      speckleFi.displayValue = GetElementMesh(revitFi, GetAllFamSubElements(revitFi));

      var material = ConverterRevit.GetMEPSystemMaterial(revitFi);

      if (material != null)
        foreach (var mesh in speckleFi.displayValue)
          mesh["renderMaterial"] = material;

      GetAllRevitParamsAndIds(speckleFi, revitFi);

#region sub elements capture

      var subElementIds = revitFi.GetSubComponentIds();
      var convertedSubElements = new List<Base>();

      foreach (var elemId in subElementIds)
      {
        var subElem = revitFi.Document.GetElement(elemId);
        if (CanConvertToSpeckle(subElem))
        {
          var obj = ConvertToSpeckle(subElem);

          if (obj != null)
          {
            convertedSubElements.Add(obj);
            ConvertedObjectsList.Add(obj.applicationId);
          }
        }
      }

      if (convertedSubElements.Any())
      {
        speckleFi.elements = convertedSubElements;
      }

#endregion

      // TODO:
      // revitFi.GetSubelements();
      return speckleFi;
    }

    /// <summary>
    /// Note: not tested. Not sure what the scenarios here would be either (super families?)
    /// </summary>
    /// <param name="familyInstance"></param>
    /// <returns></returns>
    private List<DB.Element> GetAllFamSubElements(DB.FamilyInstance familyInstance)
    {
      var subElements = new List<DB.Element>();
      foreach (var id in familyInstance.GetSubComponentIds())
      {
        var element = familyInstance.Document.GetElement(id);
        subElements.Add(element);
        if (element is Autodesk.Revit.DB.FamilyInstance)
          subElements.AddRange(GetAllFamSubElements(element as DB.FamilyInstance));
      }
      return subElements;
    }

    private double ComputePlaneDistance(XYZ planeOrigin, XYZ planeNormal, XYZ point)
    {
      // D = nx*ox + ny+oy nz+oz
      // where planeNormal = {nx,ny,nz} and planeOrigin = {ox,oy,oz}
      double D = planeNormal.X * planeOrigin.X + planeNormal.Y * planeOrigin.Y + planeNormal.Z * planeOrigin.Z;
      double PointD = planeNormal.X * point.X + planeNormal.Y * point.Y + planeNormal.Z * point.Z;
      double value = Math.Abs(D - PointD);

      return value;
    }

    private bool NormalsAlign(XYZ normal1, XYZ normal2)
    {
      var isXNormAligned = Math.Abs(Math.Abs(normal1.X) - Math.Abs(normal2.X)) < TOLERANCE;
      var isYNormAligned = Math.Abs(Math.Abs(normal1.Y) - Math.Abs(normal2.Y)) < TOLERANCE;
      var isZNormAligned = Math.Abs(Math.Abs(normal1.Z) - Math.Abs(normal2.Z)) < TOLERANCE;

      return isXNormAligned && isYNormAligned && isZNormAligned;
    }
  }
}
