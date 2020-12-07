using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Entry point for all revit family conversions. TODO: Check for Beams and Columns and any other "dedicated" speckle elements and convert them as such rather than to the generic "family instance" object.
    /// </summary>
    /// <param name="myElement"></param>
    /// <returns></returns>
    public Base FamilyInstanceToSpeckle(DB.FamilyInstance revitFi)
    {

      #region host handling

      // Check if it's been converted previously - from a parent host.
      if (ConvertedObjectsList.IndexOf(revitFi.UniqueId) != -1)
      {
        return null;
      }

      // If the parent is in our selection list, back off, as this element will be converted by the host element.
      if (revitFi.Host != null && ContextObjects.FindIndex(obj => obj.applicationId == revitFi.Host.UniqueId) != -1)
      {
        return null;
      }

      #endregion

      //adaptive components
      if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(revitFi))
      {
        return AdaptiveComponentToSpeckle(revitFi);
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
        throw new Exception("Only point based Family Instances are currently supported.");
      }

      //anything else
      var baseLevelParam = revitFi.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
      var baseLevelParam2 = revitFi.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var subElements = GetFamSubElements(revitFi);

      var speckleFi = new BuiltElements.Revit.FamilyInstance();
      speckleFi.basePoint = basePoint;
      speckleFi.type = Doc.GetElement(revitFi.GetTypeId()).Name;
      speckleFi.facingFlipped = revitFi.FacingFlipped;
      speckleFi.handFlipped = revitFi.HandFlipped;
      speckleFi.level = ConvertAndCacheLevel(baseLevelParam);

      if (revitFi.Location is LocationPoint)
      {
        speckleFi.rotation = ((LocationPoint)revitFi.Location).Rotation;
      }

      speckleFi["@displayMesh"] = GetElementMesh(revitFi, subElements);

      AddCommonRevitProps(speckleFi, revitFi);

      // TODO:
      // revitFi.GetSubelements();

      return speckleFi;
    }

    /// <summary>
    /// Note: not tested. Not sure what the scenarios here would be either (super families?)
    /// </summary>
    /// <param name="familyInstance"></param>
    /// <returns></returns>
    private List<DB.Element> GetFamSubElements(DB.FamilyInstance familyInstance)
    {
      var subElements = new List<DB.Element>();
      foreach (var id in familyInstance.GetSubComponentIds())
      {
        var element = Doc.GetElement(id);
        subElements.Add(element);
        if (element is Autodesk.Revit.DB.FamilyInstance)
        {
          subElements.AddRange(GetFamSubElements(element as DB.FamilyInstance));
        }
      }
      return subElements;
    }

    //TODO: might need to clean this up and split the logic by beam, FI, etc...
    public List<ApplicationPlaceholderObject> FamilyInstanceToNative(BuiltElements.Revit.FamilyInstance speckleFi)
    {
      DB.FamilySymbol familySymbol = GetElementType<FamilySymbol>(speckleFi);
      XYZ basePoint = PointToNative(speckleFi.basePoint);
      DB.Level level = LevelToNative(speckleFi.level);
      DB.FamilyInstance familyInstance = null;

      //try update existing
      var docObj = GetExistingElementByApplicationId(speckleFi.applicationId);
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
            (familyInstance.Location as LocationPoint).Point = basePoint;

            // check for a type change
            if (speckleFi.type != null && speckleFi.type != revitType.Name)
            {
              familyInstance.ChangeTypeId(familySymbol.Id);
            }

            //some elements us the Level param, otehrs the Reference Level param (eg beams)
          }
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

      Doc.Regenerate();

      TrySetParam(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, level);

      if (familyInstance.CanFlipHand && speckleFi.handFlipped != familyInstance.HandFlipped)
      {
        familyInstance.flipHand();
      }

      if (familyInstance.CanFlipFacing && speckleFi.facingFlipped != familyInstance.FacingFlipped)
      {
        familyInstance.flipFacing();
      }

      if (familyInstance.CanRotate && speckleFi.rotation != (familyInstance.Location as LocationPoint).Rotation)
      {
        var axis = DB.Line.CreateBound(new XYZ(basePoint.X, basePoint.Y, 0), new XYZ(basePoint.X, basePoint.Y, 1000));
        (familyInstance.Location as LocationPoint).Rotate(axis, speckleFi.rotation - (familyInstance.Location as LocationPoint).Rotation);
      }
        

      SetInstanceParameters(familyInstance, speckleFi);

      var placeholders = new List<ApplicationPlaceholderObject>() {
        new ApplicationPlaceholderObject {
          applicationId = speckleFi.applicationId,
          ApplicationGeneratedId = familyInstance.UniqueId,
          NativeObject = familyInstance
        }
      };

      return placeholders;
    }
  }
}