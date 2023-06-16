using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.Interfaces;
using Objects.Other.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    #region ToNative

    public const string faceFlipFailErrorMsg = "This element should be facing flipped, but a Revit API limitation prevented this from happening programmatically";
    public const string handFlipFailErrorMsg = "This element should be hand flipped, but a Revit API limitation prevented this from happening programmatically";
    public ApplicationObject RevitInstanceToNative<TFamilyInstance>(TFamilyInstance instance)
      where TFamilyInstance : Base, IRevitFamilyInstance
    {
      DB.FamilyInstance familyInstance = null;
      var docObj = GetExistingElementByApplicationId(instance.applicationId);
      var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };
      var isUpdate = false;

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      FamilySymbol familySymbol;
      bool isExactMatch;
      if (instance is Objects.Other.Instance speckleInstance)
      {
        var definition = speckleInstance.definition as RevitSymbolElementType;
        familySymbol = GetElementType<FamilySymbol>(definition, appObj, out isExactMatch);
      }
      else
      {
        familySymbol = GetElementType<FamilySymbol>(instance, appObj, out isExactMatch);
      }
      if (familySymbol == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      // get the transform, insertion point, level, and placement type of the instance
      var transform = TransformToNative(instance.transform);
      DB.Level level = ConvertLevelToRevit(instance.level, out ApplicationObject.State levelState);
      var insertionPoint = transform.OfPoint(XYZ.Zero);

      transform = GetEditedTransformForHandAndFaceFlipping(transform, instance.handFlipped, instance.facingFlipped);

      if (!Enum.TryParse(instance.placementType, true, out FamilyPlacementType placementType))
      {
        placementType = FamilyPlacementType.Invalid;
      }

      // check for existing and update if so
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

            var newLocationPoint = new XYZ(
              insertionPoint.X,
              insertionPoint.Y,
              (familyInstance.Location as LocationPoint).Point.Z
            );
            (familyInstance.Location as LocationPoint).Point = newLocationPoint;

            // check for a type change
            if (isExactMatch && revitType.Id.IntegerValue != familySymbol.Id.IntegerValue)
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
        switch (placementType)
        {
          case FamilyPlacementType.OneLevelBasedHosted when CurrentHostElement != null:
            familyInstance = Doc.Create.NewFamilyInstance(
              insertionPoint,
              familySymbol,
              CurrentHostElement,
              level,
              StructuralType.NonStructural
            );
            break;

          case FamilyPlacementType.WorkPlaneBased when CurrentHostElement != null:
            Options op = new Options() { ComputeReferences = true };
            GeometryElement geomElement = CurrentHostElement.get_Geometry(op);
            if (geomElement == null)
            {
              // if host geom was null, then regenerate document and that should fix it
              Doc.Regenerate();
              geomElement = CurrentHostElement.get_Geometry(op);
              // if regenerating didn't fix it then try generic method
              // TODO: this won't be correct, maybe we should just throw an error?
              if (geomElement == null)
              {
                goto default;
              }
            }
            Reference faceRef = null;
            var planeDist = double.MaxValue;
            GetReferencePlane(geomElement, insertionPoint, ref faceRef, ref planeDist);
            XYZ norm = new XYZ(0, 0, 0);
            try
            {
              familyInstance = Doc.Create.NewFamilyInstance(faceRef, insertionPoint, norm, familySymbol);
            }
            catch (Exception e)
            {
              appObj.Update(
                status: ApplicationObject.State.Failed,
                logItem: $"Could not create WorkPlaneBased hosted instance: {e.Message}"
              );
              return appObj;
            }
            // parameters
            IList<DB.Parameter> cutVoidsParams = familySymbol.Family.GetParameters("Cut with Voids When Loaded");
            IList<DB.Parameter> lvlParams = familyInstance.GetParameters("Schedule Level");
            if (cutVoidsParams.ElementAtOrDefault(0) != null && cutVoidsParams[0].AsInteger() == 1)
              InstanceVoidCutUtils.AddInstanceVoidCut(Doc, CurrentHostElement, familyInstance);
            if (lvlParams.ElementAtOrDefault(0) != null && level != null)
              lvlParams[0].Set(level.Id);
            break;

          case FamilyPlacementType.OneLevelBased when CurrentHostElement is FootPrintRoof roof: // handle receiving mullions on a curtain roof
            var curtainGrids = roof.CurtainGrids;
            CurtainGrid lastGrid = null;
            foreach (var curtainGrid in curtainGrids)
              if (curtainGrid is CurtainGrid c)
                lastGrid = c;
            var isUGridLine = instance["isUGridLine"] as bool? != null ? (bool)instance["isUGridLine"] : false;
            if (lastGrid != null && isUGridLine)
            {
              var gridLine = lastGrid.AddGridLine(isUGridLine, insertionPoint, false);
              foreach (var seg in gridLine.AllSegmentCurves)
                gridLine.AddMullions(seg as Curve, familySymbol as MullionType, isUGridLine);
            }
            break;

          default:
            familyInstance = Doc.Create.NewFamilyInstance(
              insertionPoint,
              familySymbol,
              level,
              StructuralType.NonStructural
            );
            break;
        }
      }

      if (familyInstance == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create instance");
        return appObj;
      }

      //Doc.Regenerate();

      UpdateInstanceFaceAndHandFlipping(instance, familyInstance, appObj, transform, insertionPoint);

      // required to get correct transform after flipping and mirroring
      Doc.Regenerate();

      UpdateInstanceRotation(familyInstance, appObj, transform);
      UpdateInstanceLocation(familyInstance, insertionPoint);

      SetInstanceParameters(familyInstance, instance);
      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: familyInstance.UniqueId, convertedItem: familyInstance);
      appObj = SetHostedElements(instance, familyInstance, appObj);
      return appObj;
    }

    private void UpdateInstanceFaceAndHandFlipping(IRevitFamilyInstance instance, DB.FamilyInstance familyInstance, ApplicationObject appObj, Transform transform, XYZ insertionPoint)
    {
      if (instance.handFlipped != familyInstance.HandFlipped)
      {
        if (familyInstance.CanFlipHand)
        {
          familyInstance.flipHand();
        }
        else
        {
          Doc.Regenerate();
          // sometimes you can flip the hand of facing via mirroring the element around an axis, so try that
          using var subt = new SubTransaction(Doc);
          subt.Start();
          MirrorAroundAxis(familyInstance, transform.BasisX, insertionPoint);
          if (instance.handFlipped == familyInstance.HandFlipped)
          {
            subt.Commit();
          }
          else
          {
            subt.RollBack();
            appObj.Update(logItem: handFlipFailErrorMsg);
          }
        }
      }


      if (instance.facingFlipped != familyInstance.FacingFlipped)
      {
        if (familyInstance.CanFlipFacing)
        {
          familyInstance.flipFacing();
        }
        else
        {
          Doc.Regenerate();
          // sometimes you can flip the hand of facing via mirroring the element around an axis, so try that
          using var subt = new SubTransaction(Doc);
          subt.Start();
          MirrorAroundAxis(familyInstance, transform.BasisY, insertionPoint);
          if (instance.facingFlipped == familyInstance.FacingFlipped)
          {
            subt.Commit();
          }
          else
          {
            subt.RollBack();
            appObj.Update(logItem: faceFlipFailErrorMsg);
          }
        }
      }
    }

    private static void UpdateInstanceLocation(DB.FamilyInstance familyInstance, XYZ insertionPoint)
    {
      var currentTransform = familyInstance.GetTotalTransform();

      ElementTransformUtils.MoveElement(familyInstance.Document,
        familyInstance.Id,
        new XYZ(
          insertionPoint.X - currentTransform.Origin.X,
          insertionPoint.Y - currentTransform.Origin.Y,
          insertionPoint.Z - currentTransform.Origin.Z)
        );
    }

    private static Transform UpdateInstanceRotation(DB.FamilyInstance familyInstance, ApplicationObject appObj, Transform transform)
    {
      var currentTransform = GetTransformThatConsidersHandAndFaceFlipping(familyInstance);
      double rotation = GetSignedRotation(transform, currentTransform);

      if (Math.Abs(rotation) > TOLERANCE && familyInstance.Location is LocationPoint location)
      {
        try // some point based families don't have a rotation, so keep this in a try catch
        {
          using var axis = DB.Line.CreateUnbound(location.Point, currentTransform.BasisZ);
          location.Rotate(axis, -rotation);
        }
        catch (Exception e)
        {
          appObj.Update(logItem: $"Could not rotate created instance: {e.Message}");
        }
      }

      return currentTransform;
    }

    private void MirrorAroundAxis(DB.FamilyInstance familyInstance, XYZ planeNormal, XYZ insertionPoint)
    {
      // mirroring
      // note: mirroring a hosted instance via api will fail, thanks revit: there is workaround hack to group the element -> mirror -> ungroup
      Group group = CurrentHostElement != null ? Doc.Create.NewGroup(new[] { familyInstance.Id }) : null;
      var elementToMirror = group != null ? new[] { group.Id } : new[] { familyInstance.Id };

      try
      {
        ElementTransformUtils.MirrorElements(
          Doc,
          elementToMirror,
          DB.Plane.CreateByNormalAndOrigin(planeNormal, insertionPoint),
          false
        );
      }
      catch (Exception e)
      {
      }
      group?.UngroupMembers();
    }

    public static double GetSignedRotation(Transform desiredTransform, Transform actualTransform)
    {
      var desiredBasisX = new Vector(desiredTransform.BasisX.X, desiredTransform.BasisX.Y, desiredTransform.BasisX.Z);
      var currentBasisX = new Vector(actualTransform.BasisX.X, actualTransform.BasisX.Y, actualTransform.BasisX.Z);

      var rotation = Math.Atan2(
        Vector.DotProduct(Vector.CrossProduct(desiredBasisX, currentBasisX),
        new Vector(actualTransform.BasisZ.X, actualTransform.BasisZ.Y, actualTransform.BasisZ.Z)),
        Vector.DotProduct(desiredBasisX, currentBasisX)
      );
      return rotation;
    }

    public static Transform GetTransformThatConsidersHandAndFaceFlipping(DB.FamilyInstance fi)
    {
      var transform = fi.GetTotalTransform();
      return GetEditedTransformForHandAndFaceFlipping(transform, fi.HandFlipped, fi.FacingFlipped);
    }

    /// <summary>
    /// For some reason I'll never understand, the reflection of Revit elements DOES NOT show up in the element's transform
    /// <para>https://forums.autodesk.com/t5/revit-api-forum/gettransform-does-not-include-reflection-into-the-transformation/m-p/10334547</para>
    /// therefore we need to adjust the desired transform to reflect the flipping of the element.
    /// if the instance is either hand or facing flipped (but not both) then the transform is left handed
    /// and we need to multiply the corrosponding basis by -1
    /// </summary>
    /// <param name=""></param>
    /// <param name="fi"></param>
    public static Transform GetEditedTransformForHandAndFaceFlipping(DB.Transform transform, bool handFlipped, bool facingFlipped)
    {
      var newTransform = transform;
      if (handFlipped && !facingFlipped)
      {
        newTransform.BasisX *= -1;
      }
      if (facingFlipped && !handFlipped)
      {
        newTransform.BasisY *= -1;
      }
      return newTransform;
    }

    #endregion

    #region ToSpeckle

    public RevitInstance RevitInstanceToSpeckle(
      DB.FamilyInstance instance,
      out List<string> notes,
      Transform parentTransform,
      bool useParentTransform = false
    )
    {
      notes = new List<string>();

      // get the transform
      var instanceTransform = instance.GetTotalTransform();
      var localTransform = instanceTransform;
      if (useParentTransform) // this is a nested instance, remove the parent transform from it and don't apply doc reference point transforms
      {
        localTransform = parentTransform.Inverse.Multiply(instanceTransform);
      }
      var transform = TransformToSpeckle(localTransform, instance.Document, useParentTransform);

      // get the definition base of this instance
      RevitSymbolElementType definition = GetRevitInstanceDefinition(
        instance,
        out List<string> definitionNotes,
        instanceTransform
      );
      notes.AddRange(definitionNotes);

      var _instance = new RevitInstance();
      _instance.transform = transform;
      _instance.typedDefinition = definition;
      _instance.level = ConvertAndCacheLevel(instance, BuiltInParameter.FAMILY_LEVEL_PARAM) ?? ConvertAndCacheLevel(instance, BuiltInParameter.SCHEDULE_LEVEL_PARAM);
      _instance.facingFlipped = instance.FacingFlipped;
      _instance.handFlipped = instance.HandFlipped;
      _instance.mirrored = instance.Mirrored;

      // if a family instance is twoLevelBased, then store the top level
      if (instance.Symbol.Family.FamilyPlacementType == FamilyPlacementType.TwoLevelsBased)
      {
        _instance["topLevel"] = ConvertAndCacheLevel(instance, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
        _instance["topLevel"] ??= ConvertAndCacheLevel(instance, BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM);
      }

      GetAllRevitParamsAndIds(_instance, instance);

      return _instance;
    }

    /// <summary>
    /// Converts the familysymbol into a revitsymbolelementtype
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="notes"></param>
    /// <param name="parentTransform"></param>
    /// <returns></returns>
    /// <remarks>TODO: could potentially optimize this for symbols with the same displayvalues by caching previously converted symbols</remarks>
    private RevitSymbolElementType GetRevitInstanceDefinition(
      DB.FamilyInstance instance,
      out List<string> notes,
      Transform parentTransform
    )
    {
      notes = new List<string>();
      var symbol =
        ElementTypeToSpeckle(instance.Document.GetElement(instance.GetTypeId()) as ElementType)
        as RevitSymbolElementType;
      if (symbol == null)
      {
        notes.Add($"Could not convert element type as FamilySymbol");
        return null;
      }

      // get the displayvalue of the family symbol
      try
      {
        var meshes = GetElementDisplayValue(instance, new Options() { DetailLevel = ViewDetailLevel.Fine }, true);
        symbol.displayValue = meshes;
      }
      catch (Exception e)
      {
        notes.Add($"Could not retrieve display meshes: {e.Message}");
      }

      #region sub elements capture

      var subElementIds = instance.GetSubComponentIds();
      var convertedSubElements = new List<Base>();

      foreach (var elemId in subElementIds)
      {
        var subElem = instance.Document.GetElement(elemId);
        Base converted = null;
        switch (subElem)
        {
          case DB.FamilyInstance o:
            converted = RevitInstanceToSpeckle(o, out notes, parentTransform, true);
            if (converted == null)
              goto default;
            break;
          default:
            converted = ConvertToSpeckle(subElem);
            break;
        }
        if (converted != null)
        {
          convertedSubElements.Add(converted);
          ConvertedObjects.Add(converted.applicationId);
        }
      }

      if (convertedSubElements.Any())
      {
        symbol.elements = convertedSubElements;
      }
      #endregion

      var material = ConverterRevit.GetMEPSystemMaterial(instance);

      if (material != null)
        foreach (var mesh in symbol.displayValue)
          mesh["renderMaterial"] = material;

      return symbol;
    }
    #endregion
  }
}
