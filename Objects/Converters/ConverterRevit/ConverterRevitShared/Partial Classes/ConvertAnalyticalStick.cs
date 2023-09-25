using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Structure.StructuralSections;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject AnalyticalStickToNative(Element1D speckleStick)
    {
      ApplicationObject appObj = null;
      XYZ offset1 = VectorToNative(speckleStick.end1Offset ?? new Geometry.Vector(0, 0, 0));
      XYZ offset2 = VectorToNative(speckleStick.end2Offset ?? new Geometry.Vector(0, 0, 0));

#if REVIT2020 || REVIT2021 || REVIT2022
      appObj = CreatePhysicalMember(speckleStick);
      DB.FamilyInstance physicalMember = (DB.FamilyInstance)appObj.Converted.FirstOrDefault();
      SetAnalyticalProps(physicalMember, speckleStick, offset1, offset2);
#else
      var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);

      // check for existing member
      var docObj = GetExistingElementByApplicationId(speckleStick.applicationId);
      appObj = new ApplicationObject(speckleStick.id, speckleStick.speckle_type) { applicationId = speckleStick.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      if (speckleStick.baseLine == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Only line based Analytical Members are currently supported.");
        return appObj;
      }

      var baseLine = CurveToNative(speckleStick.baseLine).get_Item(0);
      DB.Level level = null;

      level ??= ConvertLevelToRevit(LevelFromCurve(baseLine), out ApplicationObject.State levelState);
      var isUpdate = false;

      var familySymbol = GetElementType<FamilySymbol>(speckleStick, appObj, out bool isExactMatch);
      if (familySymbol == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      AnalyticalMember revitMember = null;
      DB.FamilyInstance physicalMember = null;

      if (docObj != null && docObj is AnalyticalMember analyticalMember)
      {
        // update location
        var currentCurve = analyticalMember.GetCurve();
        var p0 = currentCurve.GetEndPoint(0);

        if (p0.DistanceTo(baseLine.GetEndPoint(0)) > p0.DistanceTo(baseLine.GetEndPoint(1)))
          analyticalMember.SetCurve(baseLine.CreateReversed());
        else
          analyticalMember.SetCurve(baseLine);

        if (isExactMatch)
        {
          //update type
          if (familySymbol.Category.EqualsBuiltInCategory(BuiltInCategory.OST_StructuralColumns)
            || familySymbol.Category.EqualsBuiltInCategory(BuiltInCategory.OST_StructuralFraming))
          {
            analyticalMember.SectionTypeId = familySymbol.Id;
          }
          isUpdate = true;
          revitMember = analyticalMember;

          if (analyticalToPhysicalManager.HasAssociation(revitMember.Id))
          {
            var physicalMemberId = analyticalToPhysicalManager.GetAssociatedElementId(revitMember.Id);
            physicalMember = (DB.FamilyInstance)Doc.GetElement(physicalMemberId);
            if (physicalMember.Symbol != familySymbol)
              physicalMember.Symbol = familySymbol;
          }
        }
      }

      //create family instance
      if (revitMember == null)
      {
        revitMember = AnalyticalMember.Create(Doc, baseLine);
        //set type
        if (familySymbol.Category.EqualsBuiltInCategory(BuiltInCategory.OST_StructuralColumns)
            || familySymbol.Category.EqualsBuiltInCategory(BuiltInCategory.OST_StructuralFraming))
        {
          revitMember.SectionTypeId = familySymbol.Id;
        }
      }

      // set or update analytical properties
      SetAnalyticalProps(revitMember, speckleStick, offset1, offset2);

      // if there isn't an associated physical element to the analytical element, create it
      if (!analyticalToPhysicalManager.HasAssociation(revitMember.Id))
      {
        var physicalMemberAppObj = CreatePhysicalMember(speckleStick);
        physicalMember = (DB.FamilyInstance)physicalMemberAppObj.Converted.FirstOrDefault();
        analyticalToPhysicalManager.AddAssociation(revitMember.Id, physicalMember.Id);

        appObj.Update(createdId: physicalMember.UniqueId, convertedItem: physicalMember);
      }

      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: revitMember.UniqueId, convertedItem: revitMember);
#endif
      return appObj;
    }

    private ApplicationObject CreatePhysicalMember(Element1D speckleStick)
    {
      ApplicationObject appObj = null;
      XYZ offset1 = VectorToNative(speckleStick.end1Offset ?? new Geometry.Vector(0, 0, 0));
      XYZ offset2 = VectorToNative(speckleStick.end2Offset ?? new Geometry.Vector(0, 0, 0));

      var propertyName = speckleStick.property?.name;

      //This only works for CSIC sections now for sure. Need to test on other sections
      if (!string.IsNullOrEmpty(propertyName))
        propertyName = propertyName.Replace('X', 'x');

      switch (speckleStick.type)
      {
        case ElementType1D.Beam:
          RevitBeam revitBeam = new RevitBeam();
          revitBeam.type = propertyName;
          revitBeam.baseLine = speckleStick.baseLine;
#if REVIT2020 || REVIT2021 || REVIT2022
          revitBeam.applicationId = speckleStick.applicationId;
#endif
          appObj = BeamToNative(revitBeam);

          return appObj;

        case ElementType1D.Brace:
          Brace speckleBrace = new();
          SetElementType(speckleBrace, propertyName);
          speckleBrace.baseLine = speckleStick.baseLine;
          speckleBrace.units = speckleStick.units;
#if REVIT2020 || REVIT2021 || REVIT2022
          speckleBrace.applicationId = speckleStick.applicationId;
#endif
          appObj = BraceToNative(speckleBrace);

          return appObj;

        case ElementType1D.Column:
          Column speckleColumn = new();
          SetElementType(speckleColumn, propertyName);
          speckleColumn.baseLine = speckleStick.baseLine;
          speckleColumn.units = speckleStick.units;
#if REVIT2020 || REVIT2021 || REVIT2022
          speckleColumn.applicationId = speckleStick.applicationId;
#endif
          appObj = ColumnToNative(speckleColumn);

          return appObj;
      }
      return appObj;
    }

    private void SetAnalyticalProps(Element element, Element1D element1d, XYZ offset1, XYZ offset2)
    {
      Func<char, bool> releaseConvert = rel => rel == 'R';

#if REVIT2020 || REVIT2021 || REVIT2022
      var analyticalModel = (AnalyticalModelStick)element.GetAnalyticalModel();
      analyticalModel.SetReleases(true, releaseConvert(element1d.end1Releases.code[0]), releaseConvert(element1d.end1Releases.code[1]), releaseConvert(element1d.end1Releases.code[2]), releaseConvert(element1d.end1Releases.code[3]), releaseConvert(element1d.end1Releases.code[4]), releaseConvert(element1d.end1Releases.code[5]));
      analyticalModel.SetReleases(false, releaseConvert(element1d.end2Releases.code[0]), releaseConvert(element1d.end2Releases.code[1]), releaseConvert(element1d.end2Releases.code[2]), releaseConvert(element1d.end2Releases.code[3]), releaseConvert(element1d.end2Releases.code[4]), releaseConvert(element1d.end2Releases.code[5]));
      analyticalModel.SetOffset(AnalyticalElementSelector.StartOrBase, offset1);
      analyticalModel.SetOffset(AnalyticalElementSelector.EndOrTop, offset2);
#else
      if (element is AnalyticalMember analyticalMember)
      {
        analyticalMember.SetReleaseConditions(new ReleaseConditions(true, releaseConvert(element1d.end1Releases.code[0]), releaseConvert(element1d.end1Releases.code[1]), releaseConvert(element1d.end1Releases.code[2]), releaseConvert(element1d.end1Releases.code[3]), releaseConvert(element1d.end1Releases.code[4]), releaseConvert(element1d.end1Releases.code[5])));
        analyticalMember.SetReleaseConditions(new ReleaseConditions(false, releaseConvert(element1d.end2Releases.code[0]), releaseConvert(element1d.end2Releases.code[1]), releaseConvert(element1d.end2Releases.code[2]), releaseConvert(element1d.end2Releases.code[3]), releaseConvert(element1d.end2Releases.code[4]), releaseConvert(element1d.end2Releases.code[5])));
      }
      //TODO Set offsets
#endif
    }
#if REVIT2020 || REVIT2021 || REVIT2022
    private Element1D AnalyticalStickToSpeckle(AnalyticalModelStick revitStick)
    {
      if (!revitStick.IsEnabled())
        return new Element1D();

      var speckleElement1D = new Element1D();
      switch (revitStick.Category.Name)
      {
        case "Analytical Columns":
          speckleElement1D.type = ElementType1D.Column;
          break;
        case "Analytical Beams":
          speckleElement1D.type = ElementType1D.Beam;
          break;
        case "Analytical Braces":
          speckleElement1D.type = ElementType1D.Brace;
          break;
        default:
          speckleElement1D.type = ElementType1D.Other;
          break;
      }

      var curves = revitStick.GetCurves(AnalyticalCurveType.RigidLinkHead).ToList();
      curves.AddRange(revitStick.GetCurves(AnalyticalCurveType.ActiveCurves));
      curves.AddRange(revitStick.GetCurves(AnalyticalCurveType.RigidLinkTail));

      if (curves.Count > 1)
        speckleElement1D.baseLine = null;
      else
        speckleElement1D.baseLine = CurveToSpeckle(curves[0], revitStick.Document) as Objects.Geometry.Line;


      var coordinateSystem = revitStick.GetLocalCoordinateSystem();
      if (coordinateSystem != null)
        speckleElement1D.localAxis = new Geometry.Plane(PointToSpeckle(coordinateSystem.Origin, revitStick.Document), VectorToSpeckle(coordinateSystem.BasisZ, revitStick.Document), VectorToSpeckle(coordinateSystem.BasisX, revitStick.Document), VectorToSpeckle(coordinateSystem.BasisY, revitStick.Document));

      var startOffset = revitStick.GetOffset(AnalyticalElementSelector.StartOrBase);
      var endOffset = revitStick.GetOffset(AnalyticalElementSelector.EndOrTop);
      speckleElement1D.end1Offset = VectorToSpeckle(startOffset, revitStick.Document);
      speckleElement1D.end2Offset = VectorToSpeckle(endOffset, revitStick.Document);

      SetEndReleases(revitStick, ref speckleElement1D);

      var prop = new Property1D();

      var stickFamily = (Autodesk.Revit.DB.FamilyInstance)revitStick.Document.GetElement(revitStick.GetElementId());

      var speckleSection = GetSectionProfile(stickFamily.Symbol);

      var structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.StructuralMaterialId);
      if (structMat == null)
        structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());


      prop.profile = speckleSection;
      prop.material = GetStructuralMaterial(structMat);
      prop.name = revitStick.Document.GetElement(revitStick.GetElementId()).Name;
      prop.applicationId = stickFamily.Symbol.UniqueId;

      var structuralElement = revitStick.Document.GetElement(revitStick.GetElementId());
      var mark = GetParamValue<string>(structuralElement, BuiltInParameter.ALL_MODEL_MARK);

      if (revitStick is AnalyticalModelColumn)
      {
        speckleElement1D.type = ElementType1D.Column;
        //prop.memberType = MemberType.Column;
        var locationMark = GetParamValue<string>(structuralElement, BuiltInParameter.COLUMN_LOCATION_MARK);
        if (locationMark == null)
          speckleElement1D.name = mark;
        else
          speckleElement1D.name = locationMark;
      }
      else
      {
        prop.memberType = MemberType.Beam;
        speckleElement1D.name = mark;
      }

      speckleElement1D.property = prop;

      GetAllRevitParamsAndIds(speckleElement1D, revitStick);
      speckleElement1D.displayValue = GetElementDisplayValue(revitStick.Document.GetElement(revitStick.GetElementId()));
      return speckleElement1D;
    }

#else
    private Element1D AnalyticalStickToSpeckle(AnalyticalMember revitStick)
    {
      var speckleElement1D = new Element1D();
      switch (revitStick.StructuralRole)
      {
        case AnalyticalStructuralRole.StructuralRoleColumn:
          speckleElement1D.type = ElementType1D.Column;
          break;
        case AnalyticalStructuralRole.StructuralRoleBeam:
          speckleElement1D.type = ElementType1D.Beam;
          break;
        case AnalyticalStructuralRole.StructuralRoleMember:
          speckleElement1D.type = ElementType1D.Brace;
          break;
        default:
          speckleElement1D.type = ElementType1D.Other;
          break;
      }

      speckleElement1D.baseLine = CurveToSpeckle(revitStick.GetCurve(), revitStick.Document) as Objects.Geometry.Line;

      SetEndReleases(revitStick, ref speckleElement1D);

      var prop = new Property1D();
  
      var stickFamily = (Autodesk.Revit.DB.FamilySymbol)revitStick.Document.GetElement(revitStick.SectionTypeId);

      var speckleSection = GetSectionProfile(stickFamily);

      var structMat = (DB.Material)stickFamily.Document.GetElement(revitStick.MaterialId);
      if (structMat == null)
        structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());

      prop.profile = speckleSection;
      prop.material = GetStructuralMaterial(structMat);
      prop.name = stickFamily.Name;

      var mark = GetParamValue<string>(stickFamily, BuiltInParameter.ALL_MODEL_MARK);

      //TODO: how to differenciate between column and beam?

      //if (revitStick is AnalyticalModelColumn)
      //{
      //  speckleElement1D.type = ElementType1D.Column;
      //  //prop.memberType = MemberType.Column;
      //  var locationMark = GetParamValue<string>(stickFamily, BuiltInParameter.COLUMN_LOCATION_MARK);
      //  if (locationMark == null)
      //    speckleElement1D.name = mark;
      //  else
      //    speckleElement1D.name = locationMark;
      //}
      //else
      //{
      prop.memberType = MemberType.Beam;
      speckleElement1D.name = mark;
      //}

      speckleElement1D.property = prop;

      GetAllRevitParamsAndIds(speckleElement1D, revitStick);

      var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);
      if (analyticalToPhysicalManager.HasAssociation(revitStick.Id))
      {
        var physicalElementId = analyticalToPhysicalManager.GetAssociatedElementId(revitStick.Id);
        var physicalElement = Doc.GetElement(physicalElementId);
        speckleElement1D.displayValue = GetElementDisplayValue(physicalElement);
      }

      return speckleElement1D;
    }
#endif

    private void SetEndReleases(Element revitStick, ref Element1D speckleElement1D)
    {
      var startRelease = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_START_RELEASE_TYPE);
      var endRelease = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_END_RELEASE_TYPE);
      if (startRelease == 0)
        speckleElement1D.end1Releases = new Restraint(RestraintType.Fixed);
      else
      {
        var botReleaseX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FX) == 1 ? "R" : "F";
        var botReleaseY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FY) == 1 ? "R" : "F";
        var botReleaseZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FZ) == 1 ? "R" : "F";
        var botReleaseXX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MX) == 1 ? "R" : "F";
        var botReleaseYY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MY) == 1 ? "R" : "F";
        var botReleaseZZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MZ) == 1 ? "R" : "F";

        string botReleaseCode = botReleaseX + botReleaseY + botReleaseZ + botReleaseXX + botReleaseYY + botReleaseZZ;
        speckleElement1D.end1Releases = new Restraint(botReleaseCode);
      }

      if (endRelease == 0)
        speckleElement1D.end2Releases = new Restraint(RestraintType.Fixed);
      else
      {
        var topReleaseX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_FX) == 1 ? "R" : "F";
        var topReleaseY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_FY) == 1 ? "R" : "F";
        var topReleaseZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_FZ) == 1 ? "R" : "F";
        var topReleaseXX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_MX) == 1 ? "R" : "F";
        var topReleaseYY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_MY) == 1 ? "R" : "F";
        var topReleaseZZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_MZ) == 1 ? "R" : "F";

        string topReleaseCode = topReleaseX + topReleaseY + topReleaseZ + topReleaseXX + topReleaseYY + topReleaseZZ;
        speckleElement1D.end2Releases = new Restraint(topReleaseCode);
      }
    }

    private SectionProfile GetSectionProfile(FamilySymbol familySymbol)
    {
      var revitSection = familySymbol.GetStructuralSection();
      if (revitSection == null)
        return null;

      // check section profile cache
      if (SectionProfiles.Keys.Contains(familySymbol.Name))
        return SectionProfiles[familySymbol.Name];

      var speckleSection = new SectionProfile();

      // note to future self, the StructuralSectionGeneralShape prop is sometimes null so it isn't reliable to use
      // therefore switch on the object itself instead
      switch (revitSection)
      {

        case StructuralSectionIWelded _: // Built up wide flange
        case StructuralSectionIWideFlange _: // I shaped wide flange
        case StructuralSectionGeneralI _: // General Double T shape
          speckleSection = new ISection();
          break;
        case StructuralSectionGeneralT _: // General Tee shape
          speckleSection = new Tee();
          break;
        case StructuralSectionGeneralH _: // Rectangular Pipe structural sections
        case StructuralSectionGeneralF _: // Flat Bar structural sections
          speckleSection = new Rectangular();
          break;
        case StructuralSectionGeneralR _: // Pipe structural sections
        case StructuralSectionGeneralS _: // Round Bar structural sections
          speckleSection = new Circular();
          break;
        case StructuralSectionGeneralW _: // Angle structural sections
          speckleSection = new Angle();
          break;
        case StructuralSectionGeneralU _: // Channel structural sections
          speckleSection = new Channel();
          break;

        //case StructuralSectionGeneralLA o:
        //case StructuralSectionColdFormed o:
        //case StructuralSectionUserDefined o:
        //case StructuralSectionGeneralLZ o:

        // keep these two last. They are last resorts
        case StructuralSectionRectangular _:
          speckleSection = new Rectangular();
          break;
        case StructuralSectionRound _:
          speckleSection = new Circular();
          break;
      }

      SetStructuralSectionProps(revitSection, speckleSection);

      speckleSection.units = ModelUnits;
      speckleSection.name = familySymbol.Name;

      SectionProfiles.Add(familySymbol.Name, speckleSection);

      return speckleSection;
    }

    private void SetStructuralSectionProps(StructuralSection revitSection, SectionProfile speckleSection)
    {
      var scaleFactor = ScaleToSpeckle(1);
      var scaleFactor2 = scaleFactor * scaleFactor;

      //TODO we need to support setting other units than just length
      if (revitSection is StructuralSection _)
      {
        // static props
        //TODO change this prop, Iyy can mean different things
        speckleSection.Iyy = revitSection.MomentOfInertiaStrongAxis * scaleFactor2; 
        speckleSection.Izz = revitSection.MomentOfInertiaWeakAxis * scaleFactor2;
        speckleSection.weight = revitSection.NominalWeight / scaleFactor;
        speckleSection.area = revitSection.SectionArea * scaleFactor2;
        speckleSection.J = revitSection.TorsionalMomentOfInertia * scaleFactor2 * scaleFactor2;
      }
      if (revitSection is StructuralSectionRectangular rect)
      {
        // these should be a static props, not dynamic ones, but we don't know the exact type of speckleSection here
        // this may not be the best way to do this
        speckleSection["depth"] = rect.Height * scaleFactor;
        speckleSection["width"] = rect.Width * scaleFactor;

        // dynamic props
        speckleSection["centroidHorizontal"] = rect.CentroidHorizontal * scaleFactor;
        speckleSection["centroidVertical"] = rect.CentroidVertical * scaleFactor;
      }
      if (revitSection is StructuralSectionRound round)
      {
        // static props
        speckleSection["radius"] = round.Diameter / 2 * scaleFactor;

        // dynamic props
        speckleSection["centroidHorizontal"] = round.CentroidHorizontal * scaleFactor;
        speckleSection["centroidVertical"] = round.CentroidVertical * scaleFactor;
      }
      if (revitSection is StructuralSectionHotRolled hr)
      {
        // static props
        speckleSection["flangeThickness"] = hr.FlangeThickness * scaleFactor;
        speckleSection["webThickness"] = hr.WebThickness * scaleFactor;

        // dynamic props
        speckleSection["flangeThicknessLocation"] = hr.FlangeThicknessLocation * scaleFactor;
        speckleSection["webThicknessLocation"] = hr.WebThicknessLocation * scaleFactor;
        speckleSection["webFillet"] = hr.WebFillet;
      }
      if (revitSection is StructuralSectionColdFormed cf)
      {
        //dynamic props
        speckleSection["innerFillet"] = cf.InnerFillet * scaleFactor;
        speckleSection["wallThickness"] = cf.WallNominalThickness * scaleFactor;
        speckleSection["wallDesignThickness"] = cf.WallDesignThickness * scaleFactor;
      }
      if (revitSection is StructuralSectionGeneralI i)
      {
        // dynamic props
        speckleSection["flangeFillet"] = i.FlangeFillet;
        speckleSection["slopedFlangeAngle"] = i.SlopedFlangeAngle;
        //speckleSection["flangeToeOfFillet"] = i.FlangeToeOfFillet * scaleFactor; // this is in inches (or mm?) so it needs a different scaleFactor
        //speckleSection["webToeOfFillet"] = i.WebToeOfFillet * scaleFactor; // this is in inches (or mm?) so it needs a different scaleFactor
      }
      if (revitSection is StructuralSectionGeneralT t)
      {
        speckleSection["flangeFillet"] = t.FlangeFillet;
        speckleSection["slopedFlangeAngle"] = t.SlopedFlangeAngle;
        speckleSection["slopedWebAngle"] = t.SlopedWebAngle;
        //speckleSection["flangeToeOfFillet"] = i.FlangeToeOfFillet * scaleFactor; // this is in inches (or mm?) so it needs a different scaleFactor
        //speckleSection["webToeOfFillet"] = i.WebToeOfFillet * scaleFactor; // this is in inches (or mm?) so it needs a different scaleFactor
      }
      if (revitSection is StructuralSectionGeneralH h)
      {
        // static props
        speckleSection["webThickness"] = h.WallNominalThickness * scaleFactor;
        speckleSection["flangeThickness"] = h.WallNominalThickness * scaleFactor;

        //dynamic props
        speckleSection["innerFillet"] = h.InnerFillet;
        speckleSection["outerFillet"] = h.OuterFillet;
      }
      if (revitSection is StructuralSectionGeneralR r)
      {
        // static props 
        speckleSection["wallThickness"] = r.WallNominalThickness * scaleFactor;

        //dynamic props
        speckleSection["wallDesignThickness"] = r.WallDesignThickness * scaleFactor;
      }
      if (revitSection is StructuralSectionGeneralW w)
      {
        // dynamic props
        speckleSection["flangeFillet"] = w.FlangeFillet;
        speckleSection["topWebFillet"] = w.TopWebFillet;
      }
      if (revitSection is StructuralSectionGeneralU u)
      {
        // dynamic props
        speckleSection["flangeFillet"] = u.FlangeFillet;
        speckleSection["slopedFlangeAngle"] = u.SlopedFlangeAngle;
        //speckleSection["flangeToeOfFillet"] = u.FlangeToeOfFillet * scaleFactor; // this is in inches (or mm?) so it needs a different scaleFactor
        //speckleSection["webToeOfFillet"] = u.WebToeOfFillet * scaleFactor; // this is in inches (or mm?) so it needs a different scaleFactor
      }
    }

    private StructuralMaterial GetStructuralMaterial(Material material)
    {
      if (material == null)
        return null;

      StructuralAsset materialAsset = null;
      string name = null;
      if (material.StructuralAssetId != ElementId.InvalidElementId)
      {
        materialAsset = ((PropertySetElement)material.Document.GetElement(material.StructuralAssetId)).GetStructuralAsset();

        name = material.Document.GetElement(material.StructuralAssetId)?.Name;
      }
      var materialName = material.MaterialClass;
      var materialType = GetMaterialType(materialName);

      var speckleMaterial = GetStructuralMaterial(materialType, materialAsset, name);
      speckleMaterial.applicationId = material.UniqueId;

      return speckleMaterial;
    }

    private StructuralMaterial GetStructuralMaterial(StructuralMaterialType materialType, StructuralAsset materialAsset, string name)
    {
      Structural.Materials.StructuralMaterial speckleMaterial = null;

      if (materialType == StructuralMaterialType.Undefined && materialAsset != null)
        materialType = GetMaterialType(materialAsset);

      name ??= materialType.ToString();
      switch (materialType)
      {
        case StructuralMaterialType.Concrete:
          var concreteMaterial = new Concrete
          {
            name = name,
            materialType = Structural.MaterialType.Concrete,
          };

          if (materialAsset != null)
          {
            concreteMaterial.compressiveStrength = materialAsset.ConcreteCompression; // Newtons per foot meter
            concreteMaterial.lightweight = materialAsset.Lightweight;
          }

          speckleMaterial = concreteMaterial;
          break;
        case StructuralMaterialType.Steel:
          var steelMaterial = new Steel
          {
            name = name,
            materialType = Structural.MaterialType.Steel,
            designCode = null,
            codeYear = null,
            maxStrain = 0,
            dampingRatio = 0,
          };

          if (materialAsset != null)
          {
            steelMaterial.grade = materialAsset.Name;
            steelMaterial.yieldStrength = materialAsset.MinimumYieldStress; // Newtons per foot meter
            steelMaterial.ultimateStrength = materialAsset.MinimumTensileStrength; // Newtons per foot meter
          }

          speckleMaterial = steelMaterial;
          break;
        case StructuralMaterialType.Wood:
          var timberMaterial = new Timber
          {
            name = name,
            materialType = Structural.MaterialType.Timber,
            designCode = null,
            codeYear = null,
            dampingRatio = 0
          };

          if (materialAsset != null)
          {
            timberMaterial.grade = materialAsset.WoodGrade;
            timberMaterial.species = materialAsset.WoodSpecies;
            timberMaterial["bendingStrength"] = materialAsset.WoodBendingStrength;
            timberMaterial["parallelCompressionStrength"] = materialAsset.WoodParallelCompressionStrength;
            timberMaterial["parallelShearStrength"] = materialAsset.WoodParallelShearStrength;
            timberMaterial["perpendicularCompressionStrength"] = materialAsset.WoodPerpendicularCompressionStrength;
            timberMaterial["perpendicularShearStrength"] = materialAsset.WoodPerpendicularShearStrength;
          }

          speckleMaterial = timberMaterial;
          break;
        default:
          var defaultMaterial = new Objects.Structural.Materials.StructuralMaterial
          {
            name = name,
          };
          speckleMaterial = defaultMaterial;
          break;
      }

      // TODO: support non-isotropic materials
      if (materialAsset != null)
      {
        // some of these are actually the dumbest units I've ever heard of
        speckleMaterial.elasticModulus = materialAsset.YoungModulus.X; // Newtons per foot meter
        speckleMaterial.poissonsRatio = materialAsset.PoissonRatio.X; // Unitless
        speckleMaterial.shearModulus = materialAsset.ShearModulus.X; // Newtons per foot meter
        speckleMaterial.density = materialAsset.Density; // kilograms per cubed feet 
        speckleMaterial.thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X; // inverse Kelvin
      }

      return speckleMaterial;
    }

    private StructuralMaterialType GetMaterialType(string materialName)
    {
      StructuralMaterialType materialType = StructuralMaterialType.Undefined;
      switch (materialName.ToLower())
      {
        case "concrete":
          materialType = StructuralMaterialType.Concrete;
          break;
        case "steel":
          materialType = StructuralMaterialType.Steel;
          break;
        case "wood":
          materialType = StructuralMaterialType.Wood;
          break;
      }

      return materialType;
    }

    private StructuralMaterialType GetMaterialType(StructuralAsset materialAsset)
    {
      StructuralMaterialType materialType = StructuralMaterialType.Undefined;
      switch (materialAsset?.StructuralAssetClass)
      {
        case StructuralAssetClass.Metal:
          materialType = StructuralMaterialType.Steel;
          break;
        case StructuralAssetClass.Concrete:
          materialType = StructuralMaterialType.Concrete;
          break;
        case StructuralAssetClass.Wood:
          materialType = StructuralMaterialType.Wood;
          break;
      }

      return materialType;
    }
  }
}
