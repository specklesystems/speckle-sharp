using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DB = Autodesk.Revit.DB;

using Speckle.Core.Models;

using Point = Objects.Geometry.Point;
using RevitInstance = Objects.Other.Revit.RevitInstance;
using RevitSymbolElementType = Objects.BuiltElements.Revit.RevitSymbolElementType;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Entry point for all revit family conversions.
    /// </summary>
    /// <param name="revitFi"></param>
    /// <returns></returns>
    public Base FamilyInstanceToSpeckle(DB.FamilyInstance revitFi, out List<string> notes)
    {
      notes = new List<string>();
      Base @base = null;
      Base extraProps = new Base();

      if (!ShouldConvertHostedElement(revitFi, revitFi.Host, ref extraProps))
        return null;

      //adaptive components
      if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(revitFi))
        @base = AdaptiveComponentToSpeckle(revitFi);

      //these elements come when the curtain wall is generated
      //if they are contained in 'subelements' then they have already been accounted for from a wall
      //else if they are mullions then convert them as a generic family instance but add a isUGridLine prop
      if (@base == null && Categories.curtainWallSubElements.Contains(revitFi.Category))
      {
        if (SubelementIds.Contains(revitFi.Id))
          return null;
        else if (Categories.Contains(new List<BuiltInCategory> { BuiltInCategory.OST_CurtainWallMullions }, revitFi.Category))
        {
          var direction = ((DB.Line)((Mullion)revitFi).LocationCurve).Direction;
          // TODO: add support for more severly sloped mullions. This isn't very robust at the moment
          extraProps["isUGridLine"] = Math.Abs(direction.X) > Math.Abs(direction.Y) ? true : false;
        }
        else
          //TODO: sort these so we consistently get sub-elements from the wall element in case also sub-elements are sent
          SubelementIds.Add(revitFi.Id);
      }

      //beams & braces
      if (@base == null && Categories.beamCategories.Contains(revitFi.Category))
      {
        if (revitFi.StructuralType == StructuralType.Beam)
          @base = BeamToSpeckle(revitFi, out notes);
        else if (revitFi.StructuralType == StructuralType.Brace)
          @base = BraceToSpeckle(revitFi, out notes);
      }

      //columns
      if (@base == null && Categories.columnCategories.Contains(revitFi.Category) || revitFi.StructuralType == StructuralType.Column)
        @base = ColumnToSpeckle(revitFi, out notes);

      // elements
      var baseGeometry = LocationToSpeckle(revitFi);
      var basePoint = baseGeometry as Point;
      if (@base == null && basePoint == null)
        @base = RevitElementToSpeckle(revitFi, out notes);

      // point based, convert these as revit instances
      if (@base == null)
      {
        if (revitFi.Category.Name.Contains("Structural Foundation")) // don't know why, but the transforms on structural foundation elements are really messed up 
        {
          @base = PointBasedFamilyInstanceToSpeckle(revitFi, basePoint, out notes);
        }
        else
        {
          @base = RevitInstanceToSpeckle(revitFi, out notes, null);
        }
      } 

      // add additional props to base object
      foreach (var prop in extraProps.GetMembers(DynamicBaseMemberType.Dynamic).Keys)
        @base[prop] = extraProps[prop];

      return @base;
    }

    #region OLD family instancing
    //TODO: deprecate when we no longer want to support receiving old commits with the `BuiltElements.Revit.FamilyInstance` class
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

      if (!GetElementType<DB.FamilySymbol>(speckleFi, appObj, out DB.FamilySymbol familySymbol))
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
          var isUGridLine = speckleFi["isUGridLine"] as bool? != null ? (bool)speckleFi["isUGridLine"] : false;
          familyInstance = CreateHostedFamilyInstance(appObj, familySymbol, basePoint, level, isUGridLine);
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

      if (familySymbol.Family.FamilyPlacementType == FamilyPlacementType.TwoLevelsBased && speckleFi["topLevel"] is Objects.BuiltElements.Level topLevel)
      {
        var revitTopLevel = ConvertLevelToRevit(topLevel, out ApplicationObject.State topLevelState);
        TrySetParam(familyInstance, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, revitTopLevel);
      }

      SetInstanceParameters(familyInstance, speckleFi);
      if (speckleFi.mirrored)
        appObj.Update(logItem: $"Element with id {familyInstance.Id} should be mirrored, but a Revit API limitation prevented us from doing so.");

      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: familyInstance.UniqueId, convertedItem: familyInstance);
      return appObj;
    }

    private DB.FamilyInstance CreateHostedFamilyInstance(ApplicationObject appObj, DB.FamilySymbol familySymbol, XYZ insertionPoint, DB.Level level, bool isUGridLine = false)
    {
      DB.FamilyInstance familyInstance = null;
      //If the current host element is not null, it means we're coming from inside a nested conversion. 

      if (level == null)
        level = Doc.GetElement(CurrentHostElement.LevelId) as DB.Level;

      // there are two (i think) main types of hosted elements which can be found with family.familyplacementtype
      // the two placement types for hosted elements are onelevelbasedhosted and workplanebased

      if (familySymbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBasedHosted)
      {
        familyInstance = Doc.Create.NewFamilyInstance(insertionPoint, familySymbol, CurrentHostElement, level, StructuralType.NonStructural);
      }
      else if (familySymbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased)
      {
        if (CurrentHostElement == null)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Object is work plane based but does not have a host element");
          return null;
        }
        if (CurrentHostElement is Element el)
        {
          Doc.Regenerate();

          Options op = new Options();
          op.ComputeReferences = true;
          GeometryElement geomElement = el.get_Geometry(op);
          Reference faceRef = null;
          var planeDist = double.MaxValue;

          GetReferencePlane(geomElement, insertionPoint, ref faceRef, ref planeDist);

          XYZ norm = new XYZ(0, 0, 0);
          familyInstance = Doc.Create.NewFamilyInstance(faceRef, insertionPoint, norm, familySymbol);

          // parameters
          IList<DB.Parameter> cutVoidsParams = familySymbol.Family.GetParameters("Cut with Voids When Loaded");
          IList<DB.Parameter> lvlParams = familyInstance.GetParameters("Schedule Level");

          if (cutVoidsParams.ElementAtOrDefault(0) != null && cutVoidsParams[0].AsInteger() == 1)
            InstanceVoidCutUtils.AddInstanceVoidCut(Doc, el, familyInstance);
          try
          {
            if (lvlParams.ElementAtOrDefault(0) != null)
              lvlParams[0].Set(level.Id); // this can be null
          }
          catch { }
        }
        else if (CurrentHostElement is DB.Floor floor)
        {
          // TODO: support hosted elements on floors. Should be very similar to above implementation
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Work Plane based families on floors to be supported soon");
          return null;
        }
      }
      else if (familySymbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased)
      {
        if (CurrentHostElement is FootPrintRoof roof)
        {
          // handle receiving mullions on a curtain roof
          var curtainGrids = roof.CurtainGrids;
          CurtainGrid lastGrid = null;
          foreach (var curtainGrid in curtainGrids)
            if (curtainGrid is CurtainGrid c)
              lastGrid = c;

          if (lastGrid != null && isUGridLine)
          {
            var gridLine = lastGrid.AddGridLine(isUGridLine, insertionPoint, false);
            foreach (var seg in gridLine.AllSegmentCurves)
              gridLine.AddMullions(seg as Curve, familySymbol as MullionType, isUGridLine);
          }
        }
      }
      else
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Unsupported FamilyPlacementType {familySymbol.Family.FamilyPlacementType}");
        return null;
      }
      // try a catch all solution as a last resort
      if (familyInstance == null)
      {
        try
        {
          familyInstance = Doc.Create.NewFamilyInstance(insertionPoint, familySymbol, CurrentHostElement, level, StructuralType.NonStructural);
        }
        catch { }
      }

      return familyInstance;
    }

    private Base PointBasedFamilyInstanceToSpeckle(DB.FamilyInstance revitFi, Point basePoint, out List<string> notes)
    {
      notes = new List<string>();

      var symbol = revitFi.Document.GetElement(revitFi.GetTypeId()) as DB.FamilySymbol;

      var speckleFi = new BuiltElements.Revit.FamilyInstance();
      speckleFi.basePoint = basePoint;
      speckleFi.family = symbol.FamilyName;
      speckleFi.type = symbol.Name;
      speckleFi.category = revitFi.Category.Name;
      speckleFi.facingFlipped = revitFi.FacingFlipped;
      speckleFi.handFlipped = revitFi.HandFlipped;
      speckleFi.mirrored = revitFi.Mirrored;
      speckleFi.level = ConvertAndCacheLevel(revitFi, BuiltInParameter.FAMILY_LEVEL_PARAM);
      speckleFi.level ??= ConvertAndCacheLevel(revitFi, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      speckleFi.level ??= ConvertAndCacheLevel(revitFi, BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);

      // if a family instance is twoLevelBased, then store the top level
      if (revitFi.Symbol.Family.FamilyPlacementType == FamilyPlacementType.TwoLevelsBased)
      {
        speckleFi["topLevel"] = ConvertAndCacheLevel(revitFi, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
        speckleFi["topLevel"] ??= ConvertAndCacheLevel(revitFi, BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM);
      }

      if (revitFi.Location is LocationPoint locationPoint)
        speckleFi.rotation = locationPoint.Rotation;

      speckleFi.displayValue = GetElementMesh(revitFi);

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

      return speckleFi;
    }

    #endregion

    private void GetReferencePlane(GeometryElement geomElement, XYZ basePoint, ref Reference faceRef, ref double planeDist)
    {

      foreach (var geom in geomElement)
      {
        if (geom is Solid solid)
        {
          FaceArray faceArray = solid.Faces;

          foreach (Face face in faceArray)
          {
            if (face is PlanarFace planarFace)
            {
              // some family instance base points may lie on the intersection of faces
              // this makes it so family instance families can only be placed on the
              // faces of walls
              double D = planarFace.FaceNormal.X * planarFace.Origin.X + planarFace.FaceNormal.Y * planarFace.Origin.Y + planarFace.FaceNormal.Z * planarFace.Origin.Z;
              double PointD = planarFace.FaceNormal.X * basePoint.X + planarFace.FaceNormal.Y * basePoint.Y + planarFace.FaceNormal.Z * basePoint.Z;
              double value = Math.Abs(D - PointD);
              double newPlaneDist = Math.Abs(D - PointD);
              if (newPlaneDist < planeDist)
              {
                planeDist = newPlaneDist;
                faceRef = planarFace.Reference;
              }
            }
          }
        }
        else if (geom is GeometryInstance geomInst)
        {
          GeometryElement transformedGeom = geomInst.GetInstanceGeometry(geomInst.Transform);
          GetReferencePlane(transformedGeom, basePoint, ref faceRef, ref planeDist);
        }
      }
    }

    #region new instancing



    // transforms
    private Other.Transform TransformToSpeckle(Transform transform, Document doc, bool skipDocReferencePointTransform = false)
    {
      var externalTransform = transform;

      // get the reference point transform and apply if this is a top level instance
      if (!skipDocReferencePointTransform)
      {
        var docTransform = GetDocReferencePointTransform(doc);
        externalTransform = docTransform.Inverse.Multiply(transform);
      }

      // translation
      var tX = ScaleToSpeckle(externalTransform.Origin.X, ModelUnits);
      var tY = ScaleToSpeckle(externalTransform.Origin.Y, ModelUnits);
      var tZ = ScaleToSpeckle(externalTransform.Origin.Z, ModelUnits);
      var t = new Vector(tX, tY, tZ, ModelUnits);

      // basis vectors
      var vX = new Vector(externalTransform.BasisX.X, externalTransform.BasisX.Y, externalTransform.BasisX.Z, ModelUnits);
      var vY = new Vector(externalTransform.BasisY.X, externalTransform.BasisY.Y, externalTransform.BasisY.Z, ModelUnits);
      var vZ = new Vector(externalTransform.BasisZ.X, externalTransform.BasisZ.Y, externalTransform.BasisZ.Z, ModelUnits);

      // get the scale: TODO: do revit transforms ever have scaling?
      var scale = (float)transform.Scale;

      return new Other.Transform(vX, vY, vZ, t) { units = ModelUnits };
    }

    private Transform TransformToNative(Other.Transform transform)
    {
      var _transform = new Transform(Transform.Identity);

      // translation
      if (transform.matrix.M44 == 0) return _transform;
      var tX = ScaleToNative(transform.matrix.M14 / transform.matrix.M44, transform.units);
      var tY = ScaleToNative(transform.matrix.M24 / transform.matrix.M44, transform.units);
      var tZ = ScaleToNative(transform.matrix.M34 / transform.matrix.M44, transform.units);
      var t = new XYZ(tX, tY, tZ);

      // basis vectors
      XYZ vX = new XYZ(transform.matrix.M11, transform.matrix.M21, transform.matrix.M31);
      XYZ vY = new XYZ(transform.matrix.M12, transform.matrix.M22, transform.matrix.M32);
      XYZ vZ = new XYZ(transform.matrix.M13, transform.matrix.M23, transform.matrix.M33);

      // apply to new transform
      _transform.Origin = t;
      _transform.BasisX = vX.Normalize();
      _transform.BasisY = vY.Normalize();
      _transform.BasisZ = vZ.Normalize();

      // apply doc transform
      var docTransform = GetDocReferencePointTransform(Doc);
      var internalTransform = docTransform.Multiply(_transform);

      return internalTransform;
    }

    // revit instances
    public ApplicationObject RevitInstanceToNative(RevitInstance instance)
    {
      DB.FamilyInstance familyInstance = null;
      var docObj = GetExistingElementByApplicationId(instance.applicationId);
      var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };
      var isUpdate = false;

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      // get the definition
      var definition = instance.definition as RevitSymbolElementType;
      if (!GetElementType<FamilySymbol>(definition, appObj, out FamilySymbol familySymbol))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      // get the transform, insertion point, level, and placement type of the instance
      var transform = TransformToNative(instance.transform);
      DB.Level level = ConvertLevelToRevit(instance.level, out ApplicationObject.State levelState);
      var insertionPoint = transform.OfPoint(XYZ.Zero);
      FamilyPlacementType placement = Enum.TryParse<FamilyPlacementType>(definition.placementType, true, out FamilyPlacementType placementType) ? placementType : FamilyPlacementType.Invalid;

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

            var newLocationPoint = new XYZ(insertionPoint.X, insertionPoint.Y, (familyInstance.Location as LocationPoint).Point.Z);
            (familyInstance.Location as LocationPoint).Point = newLocationPoint;

            // check for a type change
            if (definition.type != null && definition.type != revitType.Name)
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
        switch (placement)
        {
          case FamilyPlacementType.OneLevelBasedHosted when CurrentHostElement != null:
            familyInstance = Doc.Create.NewFamilyInstance(insertionPoint, familySymbol, CurrentHostElement, level, StructuralType.NonStructural);
            break;

          case FamilyPlacementType.WorkPlaneBased when CurrentHostElement != null:
            Options op = new Options() { ComputeReferences = true };
            GeometryElement geomElement = CurrentHostElement.get_Geometry(op);
            if (geomElement == null) // not sure why some wall host geom fails, try the generic method if so
            {
              goto default;
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
              appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not create WorkPlaneBased hosted instance: {e.Message}");
              return appObj;
            }
            // parameters
            IList<DB.Parameter> cutVoidsParams = familySymbol.Family.GetParameters("Cut with Voids When Loaded");
            IList<DB.Parameter> lvlParams = familyInstance.GetParameters("Schedule Level");
            if (cutVoidsParams.ElementAtOrDefault(0) != null && cutVoidsParams[0].AsInteger() == 1)
              InstanceVoidCutUtils.AddInstanceVoidCut(Doc, CurrentHostElement, familyInstance);
            try
            {
              if (lvlParams.ElementAtOrDefault(0) != null)
                lvlParams[0].Set(level.Id); // this can be null
            }
            catch { }
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
            familyInstance = Doc.Create.NewFamilyInstance(insertionPoint, familySymbol, level, StructuralType.NonStructural);
            break;
        }
      }

      if (familyInstance == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create instance");
        return appObj;
      }

      // face flipping
      Doc.Regenerate(); //required for face flipping to work!
      if (familyInstance.CanFlipHand && instance.handFlipped != familyInstance.HandFlipped)
        familyInstance.flipHand();

      if (familyInstance.CanFlipFacing && instance.facingFlipped != familyInstance.FacingFlipped)
        familyInstance.flipFacing();

      // rotation about the z axis
      var rotation = transform.BasisX.AngleTo(XYZ.BasisX);
      if (familyInstance.Location is LocationPoint location)
      {
        try // some point based families don't have a rotation, so keep this in a try catch
        {
          if (rotation != location.Rotation)
          {
            var axis = DB.Line.CreateUnbound(new XYZ(location.Point.X, location.Point.Y, 0), new XYZ(location.Point.X, location.Point.Y, 1));
            location.Rotate(axis, rotation - location.Rotation);
          }
        }
        catch (Exception e)
        {
          appObj.Update(logItem: $"Could not rotate created instance: {e.Message}");
        }
      }

      // mirroring
      // note: mirroring a hosted instance via api will fail, thanks revit: there is workaround hack to group the element -> mirror -> ungroup
      if (instance.mirrored)
      {
        Group group = CurrentHostElement != null ? Doc.Create.NewGroup(new[] { familyInstance.Id }) : null ;
        var elementToMirror = group != null ? new[] { group.Id } : new[] { familyInstance.Id };
        
        try
        {
          ElementTransformUtils.MirrorElements(Doc, elementToMirror, DB.Plane.CreateByNormalAndOrigin(transform.BasisY, insertionPoint), false);
        }
        catch (Exception e)
        {
          appObj.Update(logItem: $"Instance could not be mirrored: {e.Message}");
        }
        if (group != null)
        {
          group.UngroupMembers();
        }
      }

      SetInstanceParameters(familyInstance, instance);
      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: familyInstance.UniqueId, convertedItem: familyInstance);
      appObj = SetHostedElements(instance, familyInstance, appObj);
      return appObj;
    }

    public RevitInstance RevitInstanceToSpeckle(DB.FamilyInstance instance, out List<string> notes, Transform parentTransform, bool useParentTransform = false)
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
      RevitSymbolElementType definition = GetRevitInstanceDefinition(instance, out List<string> definitionNotes, instanceTransform);
      notes.AddRange(definitionNotes);

      var _instance = new RevitInstance();
      _instance.transform = transform;
      _instance.typedDefinition = definition;
      _instance.level = ConvertAndCacheLevel(instance, BuiltInParameter.FAMILY_LEVEL_PARAM);
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
    private RevitSymbolElementType GetRevitInstanceDefinition(DB.FamilyInstance instance, out List<string> notes, Transform parentTransform)
    {
      notes = new List<string>();
      var symbol = ElementTypeToSpeckle(instance.Document.GetElement(instance.GetTypeId()) as ElementType) as RevitSymbolElementType;
      if (symbol == null)
      {
        notes.Add($"Could not convert element type as FamilySymbol");
        return null;
      }

      // get the displayvalue of the family symbol
      try
      {
        var meshes = GetElementDisplayValue(instance, new Options() { DetailLevel = ViewDetailLevel.Fine }, true); // previous point-based family instnace conversion was using GetElementMesh(revitFi);
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
            if (converted == null) goto default;
            break;
          default:
            converted = ConvertToSpeckle(subElem);
            break;
        }
        if (converted != null)
        {
          convertedSubElements.Add(converted);
          ConvertedObjectsList.Add(converted.applicationId);
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
