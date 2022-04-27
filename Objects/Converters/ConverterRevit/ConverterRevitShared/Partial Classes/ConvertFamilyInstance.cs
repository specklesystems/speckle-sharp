using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    //TODO: might need to clean this up and split the ConversionLog.Addic by beam, FI, etc...
    public List<ApplicationPlaceholderObject> FamilyInstanceToNative(BuiltElements.Revit.FamilyInstance speckleFi)
    {
      DB.FamilySymbol familySymbol = GetElementType<FamilySymbol>(speckleFi);
      XYZ basePoint = PointToNative(speckleFi.basePoint);
      DB.Level level = ConvertLevelToRevit(speckleFi.level);
      DB.FamilyInstance familyInstance = null;
      var isUpdate = false;
      //try update existing
      var docObj = GetExistingElementByApplicationId(speckleFi.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleFi.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      }; ;
      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familySymbol.FamilyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
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
            (familyInstance.Location as LocationPoint).Point = new XYZ(basePoint.X, basePoint.Y, (familyInstance.Location as LocationPoint).Point.Z);

            // check for a type change
            if (speckleFi.type != null && speckleFi.type != revitType.Name)
            {
              familyInstance.ChangeTypeId(familySymbol.Id);
            }

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
          familyInstance = Doc.Create.NewFamilyInstance(basePoint, familySymbol, CurrentHostElement, level, StructuralType.NonStructural);
        }
        //Otherwise, proceed as normal.
        else
        {
          familyInstance = Doc.Create.NewFamilyInstance(basePoint, familySymbol, level, StructuralType.NonStructural);
        }
      }

      //required for face flipping to work!
      Doc.Regenerate();

      if (familyInstance.CanFlipHand && speckleFi.handFlipped != familyInstance.HandFlipped)
      {
        familyInstance.flipHand();
      }

      if (familyInstance.CanFlipFacing && speckleFi.facingFlipped != familyInstance.FacingFlipped)
      {
        familyInstance.flipFacing();
      }

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
      {
        Report.ConversionErrors.Add(new Exception($"Element with id {familyInstance.Id} should be mirrored, but a Revit API limitation prevented us from doing so. (speckle object id: {speckleFi.id}"));
      }

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleFi.applicationId,
        ApplicationGeneratedId = familyInstance.UniqueId,
        NativeObject = familyInstance
        }
      };
      Report.Log($"{(isUpdate ? "Updated" : "Created")} FamilyInstance ({familyInstance.Category.Name}) {familyInstance.Id}");
      return placeholders;
    }

    /// <summary>
    /// Entry point for all revit family conversions.
    /// </summary>
    /// <param name="revitFi"></param>
    /// <returns></returns>
    public Base FamilyInstanceToSpeckle(DB.FamilyInstance revitFi)
    {

      if (!ShouldConvertHostedElement(revitFi, revitFi.Host))
        return null;

      //adaptive components
      if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(revitFi))
      {
        return AdaptiveComponentToSpeckle(revitFi);
      }

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
        {
          return BeamToSpeckle(revitFi);
        }
        else if (revitFi.StructuralType == StructuralType.Brace)
        {
          return BraceToSpeckle(revitFi);
        }
      }

      //columns
      if (Categories.columnCategories.Contains(revitFi.Category) || revitFi.StructuralType == StructuralType.Column)
      {
        return ColumnToSpeckle(revitFi);
      }

      var baseGeometry = LocationToSpeckle(revitFi);
      var basePoint = baseGeometry as Point;
      if (basePoint == null)
      {
        return RevitElementToSpeckle(revitFi);
      }

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
      {
        speckleFi.rotation = ((LocationPoint)revitFi.Location).Rotation;
      }

      speckleFi.displayValue = GetElementMesh(revitFi, GetAllFamSubElements(revitFi));

      var material = ConverterRevit.GetMEPSystemMaterial(revitFi);

      if (material != null)
      {
        foreach (var mesh in speckleFi.displayValue)
        {
          mesh["renderMaterial"] = material;
        }
      }

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
      Report.Log($"Converted FamilyInstance {revitFi.Id}");
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
        {
          subElements.AddRange(GetAllFamSubElements(element as DB.FamilyInstance));
        }
      }
      return subElements;
    }
  }
}
