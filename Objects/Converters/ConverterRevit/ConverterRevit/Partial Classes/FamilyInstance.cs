using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.Revit;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Element = Objects.BuiltElements.Element;
using Level = Objects.BuiltElements.Level;
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
    public IRevitElement FamilyInstanceToSpeckle(DB.FamilyInstance revitFi)
    {
      //adaptive components
      if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(revitFi))
        return AdaptiveComponentToSpeckle(revitFi);

      //beams & braces
      if (Categories.beamCategories.Contains(revitFi.Category))
      {
        if (revitFi.StructuralType == StructuralType.Beam)
          return BeamToSpeckle(revitFi);
        else if (revitFi.StructuralType == StructuralType.Brace)
          return BraceToSpeckle(revitFi);
      }

      //columns
      if (Categories.columnCategories.Contains(revitFi.Category)
          || revitFi.StructuralType == StructuralType.Column)
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
      var subElements = GetFamSubElements(revitFi);

      var speckleFi = new RevitFamilyInstance();
      speckleFi.basePoint = basePoint;
      speckleFi.type = Doc.GetElement(revitFi.GetTypeId()).Name;
      speckleFi["facingFlipped"] = revitFi.FacingFlipped;
      speckleFi["handFlipped"] = revitFi.HandFlipped;
      if (baseLevelParam != null)
        speckleFi.level = (RevitLevel)ParameterToSpeckle(baseLevelParam);

      if (revitFi.Location is LocationPoint)
      {
        speckleFi["rotation"] = ((LocationPoint)revitFi.Location).Rotation;
      }

      speckleFi.displayMesh = MeshUtils.GetElementMesh(revitFi, Scale, subElements);

      AddCommonRevitProps(speckleFi, revitFi);


      return speckleFi;

    }

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
    public DB.FamilyInstance FamilyInstanceToNative(IRevitElement speckleElement, StructuralType structuralType = StructuralType.NonStructural)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleElement.applicationId, speckleElement.type);

      string familyName = speckleElement.family ?? "";
      DB.FamilySymbol familySymbol = GetFamilySymbol(speckleElement);
      object location = LocationToNative(speckleElement as Element);
      DB.Level level = LevelToNative(EnsureLevelExists(speckleElement.level, location));
      DB.FamilyInstance familyInstance = null;

      //try update existing 
      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
          else
          {
            familyInstance = (DB.FamilyInstance)docObj;

            //update location, if it has changed from point to curve or vice versa it's going to fail
            if (location is DB.Curve)
              (familyInstance.Location as LocationCurve).Curve = location as DB.Curve;
            else
              (familyInstance.Location as LocationPoint).Point = location as XYZ;

            // check for a type change
            if (speckleElement.type != null && speckleElement.type != revitType.Name)
              familyInstance.ChangeTypeId(familySymbol.Id);

            //some elements us the Level param, otehrs the Reference Level param (eg beams)
            TrySetParam(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, level);
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
        //hosted family instance
        if (speckleElement is RevitFamilyInstance rfi && rfi.revitHostId != 0)
        {
          var host = Doc.GetElement(new ElementId(rfi.revitHostId));
          familyInstance = Doc.Create.NewFamilyInstance(location as DB.XYZ, familySymbol, host, level, structuralType);
        }
        else if (location is DB.Curve)
          familyInstance = Doc.Create.NewFamilyInstance(location as DB.Curve, familySymbol, level, structuralType);
        else
          familyInstance = Doc.Create.NewFamilyInstance(location as XYZ, familySymbol, level, structuralType);
      }

      var elem = speckleElement as Element;

      //top level, not all family instances have it
      DB.Level topLevel = elem.HasMember<RevitLevel>("topLevel") ? LevelToNative(((RevitLevel)elem["topLevel"])) : null;
      TrySetParam(familyInstance, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, topLevel);

      //reference level, only for beams
      TrySetParam(familyInstance, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM, level);


      var handFlip = elem.GetMemberSafe<bool>("handFlipped");
      if (handFlip != familyInstance.HandFlipped)
        familyInstance.flipHand();

      var facingFlipped = elem.GetMemberSafe<bool>("facingFlipped");
      if (facingFlipped != familyInstance.FacingFlipped)
        familyInstance.flipFacing();

      if (location is XYZ)
      {
        var point = location as XYZ;
        var rotation = elem.GetMemberSafe<double>("rotation");
        var axis = DB.Line.CreateBound(new XYZ(point.X, point.Y, 0), new XYZ(point.X, point.Y, 1000));
        (familyInstance.Location as LocationPoint).Rotate(axis, rotation - (familyInstance.Location as LocationPoint).Rotation);
      }


      SetOffsets(familyInstance, elem);
      var exclusions = new List<string> { "Base Offset", "Top Offset" };
      SetElementParams(familyInstance, speckleElement, exclusions);

      return familyInstance;

    }


    /// <summary>
    /// Some families eg columns, need offsets to be set in a specific way
    /// </summary>
    /// <param name="speckleElement"></param>
    /// <param name="familyInstance"></param>
    private void SetOffsets(DB.FamilyInstance familyInstance, Element speckleElement)
    {
      var topOffsetParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      var baseOffsetParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      var baseLevelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var topLevelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

      if (topLevelParam == null || baseLevelParam == null || baseOffsetParam == null || topOffsetParam == null)
        return;


      var baseOffset = UnitUtils.ConvertToInternalUnits(speckleElement.GetMemberSafe<double>("baseOffset"), baseOffsetParam.DisplayUnitType);
      var topOffset = UnitUtils.ConvertToInternalUnits(speckleElement.GetMemberSafe<double>("topOffset"), baseOffsetParam.DisplayUnitType);

      //these have been set previously
      DB.Level level = Doc.GetElement(baseLevelParam.AsElementId()) as DB.Level;
      DB.Level topLevel = Doc.GetElement(topLevelParam.AsElementId()) as DB.Level;

      //checking if BASE offset needs to be set before or after TOP offset
      if (topLevel != null && topLevel.Elevation + baseOffset <= level.Elevation)
      {
        baseOffsetParam.Set(baseOffset);
        topOffsetParam.Set(topOffset);
      }
      else
      {
        topOffsetParam.Set(topOffset);
        baseOffsetParam.Set(baseOffset);
      }

    }





  }
}
